using FluentAssertions;
using Roivo.Application.Features.Banking.Queries.GetBankingConnectionStatus;
using Roivo.Application.Tests.Fakes;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Tests.Features.Banking;

public class GetBankingConnectionStatusHandlerTests
{
    private const string ValidAfm = "094014201";

    private sealed record Sut(
        GetBankingConnectionStatusHandler Handler,
        FakeBusinessRepository Businesses,
        FakeBankAccountRepository Accounts,
        FakeBankingCredentialStore Credentials,
        FakeTenantContext Tenant);

    private static Sut BuildSut()
    {
        var businesses = new FakeBusinessRepository();
        var accounts = new FakeBankAccountRepository();
        var credentials = new FakeBankingCredentialStore();
        var tenant = new FakeTenantContext();

        return new Sut(
            new GetBankingConnectionStatusHandler(businesses, accounts, credentials),
            businesses, accounts, credentials, tenant);
    }

    private static Business SeedBusiness(Sut sut, bool connected)
    {
        var business = Business.Create("Acme", ValidAfm, null, null);
        business.TenantId = sut.Tenant.CurrentTenantId;
        sut.Businesses.Store[business.Id] = business;

        if (connected)
        {
            business.RecordBankingConnection("Test Bank", new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc));
            sut.Credentials.Store[business.Id] = "session-1";

            var account = new BankAccount
            {
                TenantId = business.TenantId,
                BusinessId = business.Id,
                ExternalAccountUid = "uid-1",
                BankName = "Test Bank",
                Iban = "GR1601101250000000012300695",
            };
            sut.Accounts.Store[account.Id] = account;
        }

        return business;
    }

    [Fact]
    public async Task ReportsDisconnectedWhenNoSessionIsStored()
    {
        var sut = BuildSut();
        var business = SeedBusiness(sut, connected: false);

        var info = await sut.Handler.Handle(new GetBankingConnectionStatusQuery(business.Id));

        info.State.Should().Be(BankingConnectionState.Disconnected);
        info.BankName.Should().BeNull();
        info.Accounts.Should().BeEmpty();
    }

    [Fact]
    public async Task ReportsDisconnectedForAnUnknownBusiness()
    {
        var sut = BuildSut();

        var info = await sut.Handler.Handle(new GetBankingConnectionStatusQuery(Guid.NewGuid()));

        info.State.Should().Be(BankingConnectionState.Disconnected);
    }

    [Fact]
    public async Task ReportsDisconnectedForAnInactiveBusiness()
    {
        var sut = BuildSut();
        var business = SeedBusiness(sut, connected: true);
        business.Deactivate();

        var info = await sut.Handler.Handle(new GetBankingConnectionStatusQuery(business.Id));

        info.State.Should().Be(BankingConnectionState.Disconnected);
    }

    [Fact]
    public async Task ReportsConnectedWithTheBankLastSyncAndAccounts()
    {
        var sut = BuildSut();
        var business = SeedBusiness(sut, connected: true);
        var syncedAt = new DateTime(2026, 9, 29, 4, 0, 0, DateTimeKind.Utc);
        business.RecordBankingSyncSuccess(syncedAt);

        var info = await sut.Handler.Handle(new GetBankingConnectionStatusQuery(business.Id));

        info.State.Should().Be(BankingConnectionState.Connected);
        info.BankName.Should().Be("Test Bank");
        info.LastSyncAt.Should().Be(syncedAt);
        info.ConsentExpiresAt.Should().Be(new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc));
        info.Accounts.Should().ContainSingle()
            .Which.Iban.Should().Be("GR1601101250000000012300695");
    }

    [Fact]
    public async Task StaysConnectedWhileTheFailureStreakIsBelowTheThreshold()
    {
        var sut = BuildSut();
        var business = SeedBusiness(sut, connected: true);
        for (var i = 0; i < GetBankingConnectionStatusHandler.ErrorStateThreshold - 1; i++)
            business.RecordBankingSyncFailure("NetworkError");

        var info = await sut.Handler.Handle(new GetBankingConnectionStatusQuery(business.Id));

        info.State.Should().Be(BankingConnectionState.Connected,
            "a blip or two shouldn't send the user to the reconnect screen");
        info.ErrorCount.Should().Be(GetBankingConnectionStatusHandler.ErrorStateThreshold - 1);
    }

    [Fact]
    public async Task FlipsToErrorAtTheThreshold()
    {
        var sut = BuildSut();
        var business = SeedBusiness(sut, connected: true);
        for (var i = 0; i < GetBankingConnectionStatusHandler.ErrorStateThreshold; i++)
            business.RecordBankingSyncFailure("SessionExpired");

        var info = await sut.Handler.Handle(new GetBankingConnectionStatusQuery(business.Id));

        info.State.Should().Be(BankingConnectionState.Error);
        info.ErrorCount.Should().Be(GetBankingConnectionStatusHandler.ErrorStateThreshold);
        info.LastFailureReason.Should().Be("SessionExpired");
    }
}
