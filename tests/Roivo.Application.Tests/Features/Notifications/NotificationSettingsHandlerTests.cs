using FluentAssertions;
using Roivo.Application.Features.Notifications.Commands.SendTestNotification;
using Roivo.Application.Features.Notifications.Commands.UpdateNotificationSettings;
using Roivo.Application.Features.Notifications.Queries.GetNotificationSettings;
using Roivo.Application.Tests.Fakes;
using Roivo.Core.Domain.Auditing;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Tests.Features.Notifications;

/// <summary>Application-layer handler tests for the M10 notification feature.</summary>
public class NotificationSettingsHandlerTests
{
    private const string ValidAfm = "094014201";
    private const string UserId = "11111111-1111-1111-1111-111111111111";

    private sealed record Sut(
        GetNotificationSettingsHandler Get,
        UpdateNotificationSettingsHandler Update,
        SendTestNotificationHandler SendTest,
        FakeNotificationSettingsRepository Settings,
        FakeBusinessRepository Businesses,
        RecordingNotificationEmailDispatcher Dispatcher,
        FakeAuditWriter Audit,
        FakeTenantContext Tenant,
        Business Business);

    private static Sut BuildSut(TenantType tenantType = TenantType.Accountant)
    {
        var settings = new FakeNotificationSettingsRepository();
        var businesses = new FakeBusinessRepository();
        var dispatcher = new RecordingNotificationEmailDispatcher();
        var audit = new FakeAuditWriter();
        var tenant = new FakeTenantContext { CurrentTenantType = tenantType };

        var business = Business.Create("Acme", ValidAfm, null, null);
        businesses.Store[business.Id] = business;
        settings.Emails[UserId] = "owner@example.com";

        return new Sut(
            new GetNotificationSettingsHandler(settings, tenant),
            new UpdateNotificationSettingsHandler(settings, businesses, audit, tenant),
            new SendTestNotificationHandler(settings, businesses, dispatcher, audit, tenant),
            settings, businesses, dispatcher, audit, tenant, business);
    }

    private static UpdateNotificationSettingsCommand ValidUpdate(
        Guid businessId, int daysBefore = 14, string userId = UserId) => new(
            businessId,
            userId,
            DailyDigestEnabled: true,
            TaxReminderEnabled: true,
            TaxReminderDaysBefore: daysBefore,
            CashflowAlertEnabled: true,
            CashflowAlertThreshold: 500m,
            SyncFailureAlertEnabled: false,
            WeeklyReconciliationEnabled: true);

    // ----------------------------------------------------- get (defaults)

    [Fact]
    public async Task GetReturnsUnpersistedDefaultsWhenNothingSaved()
    {
        var sut = BuildSut();

        var view = await sut.Get.Handle(new GetNotificationSettingsQuery(sut.Business.Id, UserId));

        view.IsPersisted.Should().BeFalse();
        view.TaxReminderDaysBefore.Should().Be(NotificationSettings.DefaultTaxReminderDaysBefore);
        view.TaxReminderEnabled.Should().BeTrue();
        sut.Settings.Store.Should().BeEmpty("viewing settings must not write a row");
    }

    [Fact]
    public async Task GetDefaultsTurnOnDigestForAccountantsOnly()
    {
        var accountant = BuildSut(TenantType.Accountant);
        var owner = BuildSut(TenantType.Business);

        var accountantView = await accountant.Get.Handle(
            new GetNotificationSettingsQuery(accountant.Business.Id, UserId));
        var ownerView = await owner.Get.Handle(
            new GetNotificationSettingsQuery(owner.Business.Id, UserId));

        accountantView.DailyDigestEnabled.Should().BeTrue();
        accountantView.WeeklyReconciliationEnabled.Should().BeTrue();
        ownerView.DailyDigestEnabled.Should().BeFalse();
        ownerView.WeeklyReconciliationEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task GetReturnsStoredSettingsWhenPresent()
    {
        var sut = BuildSut();
        await sut.Update.Handle(ValidUpdate(sut.Business.Id, daysBefore: 21));

        var view = await sut.Get.Handle(new GetNotificationSettingsQuery(sut.Business.Id, UserId));

        view.IsPersisted.Should().BeTrue();
        view.TaxReminderDaysBefore.Should().Be(21);
        view.SyncFailureAlertEnabled.Should().BeFalse();
    }

    // ----------------------------------------------------- update (command)

    [Fact]
    public async Task UpdateCreatesTheRowOnFirstSaveAndAudits()
    {
        var sut = BuildSut();

        var result = await sut.Update.Handle(ValidUpdate(sut.Business.Id));

        result.Should().BeOfType<UpdateNotificationSettingsResult.Success>();
        sut.Settings.Store.Should().ContainKey((sut.Business.Id, UserId));
        sut.Audit.Calls.Should().ContainSingle()
            .Which.Action.Should().Be(AuditAction.NotificationSettingsUpdated);
    }

    [Fact]
    public async Task UpdateIsAnUpsertRatherThanASecondInsert()
    {
        var sut = BuildSut();

        await sut.Update.Handle(ValidUpdate(sut.Business.Id, daysBefore: 10));
        await sut.Update.Handle(ValidUpdate(sut.Business.Id, daysBefore: 30));

        sut.Settings.Store.Should().HaveCount(1);
        sut.Settings.Store[(sut.Business.Id, UserId)].TaxReminderDaysBefore.Should().Be(30);
    }

    [Fact]
    public async Task UpdateReturnsSuccessWithTheStoredValues()
    {
        var sut = BuildSut();

        var result = await sut.Update.Handle(ValidUpdate(sut.Business.Id, daysBefore: 3));

        var success = result.Should().BeOfType<UpdateNotificationSettingsResult.Success>().Subject;
        success.Settings.IsPersisted.Should().BeTrue();
        success.Settings.TaxReminderDaysBefore.Should().Be(3);
        success.Settings.CashflowAlertThreshold.Should().Be(500m);
    }

    [Fact]
    public async Task UpdateReturnsForbiddenWithoutATenantInScope()
    {
        var sut = BuildSut();
        sut.Tenant.CurrentTenantId = Guid.Empty;

        var result = await sut.Update.Handle(ValidUpdate(sut.Business.Id));

        result.Should().BeOfType<UpdateNotificationSettingsResult.Forbidden>();
        sut.Settings.Store.Should().BeEmpty();
        sut.Audit.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateReturnsNotFoundForAnUnknownBusiness()
    {
        var sut = BuildSut();

        var result = await sut.Update.Handle(ValidUpdate(Guid.NewGuid()));

        result.Should().BeOfType<UpdateNotificationSettingsResult.NotFound>();
        sut.Audit.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateReturnsNotFoundForADeactivatedBusiness()
    {
        var sut = BuildSut();
        sut.Business.Deactivate();

        var result = await sut.Update.Handle(ValidUpdate(sut.Business.Id));

        result.Should().BeOfType<UpdateNotificationSettingsResult.NotFound>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(61)]
    [InlineData(365)]
    public async Task UpdateRejectsLeadTimesOutsideOneToSixty(int daysBefore)
    {
        var sut = BuildSut();

        var result = await sut.Update.Handle(ValidUpdate(sut.Business.Id, daysBefore));

        result.Should().BeOfType<UpdateNotificationSettingsResult.Invalid>()
            .Which.Reason.Should().NotBeNullOrWhiteSpace();
        sut.Settings.Store.Should().BeEmpty();
        sut.Audit.Calls.Should().BeEmpty("a rejected update is not a thing that happened");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(60)]
    public async Task UpdateAcceptsLeadTimesAtAndInsideTheBoundaries(int daysBefore)
    {
        var sut = BuildSut();

        var result = await sut.Update.Handle(ValidUpdate(sut.Business.Id, daysBefore));

        result.Should().BeOfType<UpdateNotificationSettingsResult.Success>()
            .Which.Settings.TaxReminderDaysBefore.Should().Be(daysBefore);
    }

    [Fact]
    public async Task UpdateKeepsEachUsersPreferencesSeparate()
    {
        var sut = BuildSut();
        const string otherUser = "22222222-2222-2222-2222-222222222222";

        await sut.Update.Handle(ValidUpdate(sut.Business.Id, daysBefore: 5));
        await sut.Update.Handle(ValidUpdate(sut.Business.Id, daysBefore: 45, userId: otherUser));

        sut.Settings.Store.Should().HaveCount(2);
        sut.Settings.Store[(sut.Business.Id, UserId)].TaxReminderDaysBefore.Should().Be(5);
        sut.Settings.Store[(sut.Business.Id, otherUser)].TaxReminderDaysBefore.Should().Be(45);
    }

    // ------------------------------------------------ send test (command)

    [Fact]
    public async Task SendTestDispatchesTheChosenKindToTheUsersOwnAddress()
    {
        var sut = BuildSut();

        var result = await sut.SendTest.Handle(
            new SendTestNotificationCommand(sut.Business.Id, UserId, NotificationKind.CashflowAlert));

        result.Should().BeOfType<SendTestNotificationResult.Success>()
            .Which.RecipientEmail.Should().Be("owner@example.com");

        sut.Dispatcher.Sends.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new SampleSend(NotificationKind.CashflowAlert, "owner@example.com", sut.Business.Id, "Acme"));

        sut.Audit.Calls.Should().ContainSingle()
            .Which.Action.Should().Be(AuditAction.TestNotificationSent);
    }

    [Fact]
    public async Task SendTestReturnsForbiddenWithoutATenantInScope()
    {
        var sut = BuildSut();
        sut.Tenant.CurrentTenantId = Guid.Empty;

        var result = await sut.SendTest.Handle(
            new SendTestNotificationCommand(sut.Business.Id, UserId, NotificationKind.DailyDigest));

        result.Should().BeOfType<SendTestNotificationResult.Forbidden>();
        sut.Dispatcher.Sends.Should().BeEmpty();
    }

    [Fact]
    public async Task SendTestReturnsNotFoundForAnUnknownBusiness()
    {
        var sut = BuildSut();

        var result = await sut.SendTest.Handle(
            new SendTestNotificationCommand(Guid.NewGuid(), UserId, NotificationKind.DailyDigest));

        result.Should().BeOfType<SendTestNotificationResult.NotFound>();
        sut.Dispatcher.Sends.Should().BeEmpty();
    }

    [Fact]
    public async Task SendTestReportsTheMissingAddressRatherThanMailingNowhere()
    {
        var sut = BuildSut();
        sut.Settings.Emails.Clear();

        var result = await sut.SendTest.Handle(
            new SendTestNotificationCommand(sut.Business.Id, UserId, NotificationKind.DailyDigest));

        result.Should().BeOfType<SendTestNotificationResult.SendFailed>()
            .Which.Reason.Should().Be(SendTestNotificationResult.NoConfirmedEmailReason);
        sut.Dispatcher.Sends.Should().BeEmpty();
        sut.Audit.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task SendTestTurnsATransportFailureIntoAResult()
    {
        var sut = BuildSut();
        sut.Dispatcher.NextException = new InvalidOperationException("Resend rejected the API key");

        var result = await sut.SendTest.Handle(
            new SendTestNotificationCommand(sut.Business.Id, UserId, NotificationKind.TaxReminder));

        result.Should().BeOfType<SendTestNotificationResult.SendFailed>()
            .Which.Reason.Should().Contain("Resend rejected the API key");
        sut.Audit.Calls.Should().BeEmpty("nothing was sent, so nothing is audited");
    }
}
