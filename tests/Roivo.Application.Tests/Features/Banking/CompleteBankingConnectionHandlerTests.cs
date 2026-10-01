using FluentAssertions;
using Roivo.Application.Abstractions.Banking.Results;
using Roivo.Application.Features.Banking.Commands.CompleteBankingConnection;
using Roivo.Application.Tests.Fakes;
using Roivo.Core.Domain.Auditing;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Tests.Features.Banking;

public class CompleteBankingConnectionHandlerTests
{
    private const string ValidAfm = "094014201";
    private const string Code = "auth-code-123";

    private sealed record Sut(
        CompleteBankingConnectionHandler Handler,
        FakeBusinessRepository Businesses,
        FakeBankAccountRepository Accounts,
        FakeBankingClient Client,
        FakeBankingCredentialStore Credentials,
        FakeAuditWriter Audit,
        FakeTenantContext Tenant);

    private static Sut BuildSut()
    {
        var businesses = new FakeBusinessRepository();
        var accounts = new FakeBankAccountRepository();
        var client = new FakeBankingClient();
        var credentials = new FakeBankingCredentialStore();
        var audit = new FakeAuditWriter();
        var tenant = new FakeTenantContext();

        return new Sut(
            new CompleteBankingConnectionHandler(businesses, accounts, client, credentials, audit, tenant),
            businesses, accounts, client, credentials, audit, tenant);
    }

    private static Business SeedBusiness(Sut sut)
    {
        var business = Business.Create("Acme", ValidAfm, null, null);
        business.TenantId = sut.Tenant.CurrentTenantId;
        sut.Businesses.Store[business.Id] = business;
        return business;
    }

    [Fact]
    public async Task StoresTheSessionAndReportsTheBankOnTheHappyPath()
    {
        var sut = BuildSut();
        var business = SeedBusiness(sut);

        var result = await sut.Handler.Handle(new CompleteBankingConnectionCommand(business.Id, Code));

        var success = result.Should().BeOfType<CompleteBankingConnectionResult.Success>().Subject;
        success.BankName.Should().Be("Test Bank");
        success.AccountCount.Should().Be(1);

        sut.Client.CompleteCalls.Should().ContainSingle().Which.Should().Be(Code);
        sut.Credentials.Store[business.Id].Should().Be("session-1");
    }

    [Fact]
    public async Task CreatesABankAccountRowPerGrantedAccount()
    {
        var sut = BuildSut();
        var business = SeedBusiness(sut);
        sut.Client.NextSession = new BankingSessionResult.Success(
            "session-1", "Test Bank", "GR", null,
            [
                new BankingAccountDto("uid-1", "GR16011", "Main", "EUR", "CACC"),
                new BankingAccountDto("uid-2", "GR16022", "Savings", "EUR", "SVGS"),
            ]);

        await sut.Handler.Handle(new CompleteBankingConnectionCommand(business.Id, Code));

        sut.Accounts.Store.Values.Should().HaveCount(2);
        sut.Accounts.Store.Values.Select(a => a.ExternalAccountUid).Should().BeEquivalentTo(["uid-1", "uid-2"]);
        sut.Accounts.Store.Values.Should().OnlyContain(a => a.BusinessId == business.Id);
        sut.Accounts.Store.Values.Should().OnlyContain(a => a.TenantId == business.TenantId);
    }

    [Fact]
    public async Task ReconnectingToTheSameAccountsUpdatesRowsInsteadOfDuplicatingThem()
    {
        var sut = BuildSut();
        var business = SeedBusiness(sut);

        await sut.Handler.Handle(new CompleteBankingConnectionCommand(business.Id, Code));
        sut.Credentials.Store.Clear();
        await sut.Handler.Handle(new CompleteBankingConnectionCommand(business.Id, Code));

        sut.Accounts.Store.Values.Should().ContainSingle();
    }

    [Fact]
    public async Task RecordsTheBankAndConsentExpiryOnTheBusiness()
    {
        var sut = BuildSut();
        var business = SeedBusiness(sut);
        var validUntil = new DateTimeOffset(2026, 12, 31, 10, 0, 0, TimeSpan.Zero);
        sut.Client.NextSession = new BankingSessionResult.Success(
            "session-1", "Piraeus", "GR", validUntil,
            [new BankingAccountDto("uid-1", "GR16011", "Main", "EUR", "CACC")]);

        await sut.Handler.Handle(new CompleteBankingConnectionCommand(business.Id, Code));

        business.BankingProviderName.Should().Be("Piraeus");
        business.BankingConsentExpiresAt.Should().Be(validUntil.UtcDateTime);
    }

    [Fact]
    public async Task ASuccessfulReconnectClearsAPriorFailureStreak()
    {
        var sut = BuildSut();
        var business = SeedBusiness(sut);
        business.RecordBankingSyncFailure("SessionExpired");
        business.RecordBankingSyncFailure("SessionExpired");

        await sut.Handler.Handle(new CompleteBankingConnectionCommand(business.Id, Code));

        business.BankingSyncErrorCount.Should().Be(0);
        business.BankingLastFailureReason.Should().BeNull();
    }

    [Fact]
    public async Task FallsBackToTheAccountUidWhenTheBankSendsNoIban()
    {
        var sut = BuildSut();
        var business = SeedBusiness(sut);
        sut.Client.NextSession = new BankingSessionResult.Success(
            "session-1", "Test Bank", "GR", null,
            [new BankingAccountDto("uid-1", null, "Main", "EUR", "CACC")]);

        await sut.Handler.Handle(new CompleteBankingConnectionCommand(business.Id, Code));

        sut.Accounts.Store.Values.Single().Iban.Should().Be("uid-1");
    }

    [Fact]
    public async Task DefaultsToEuroForAnUnrecognisedCurrency()
    {
        var sut = BuildSut();
        var business = SeedBusiness(sut);
        sut.Client.NextSession = new BankingSessionResult.Success(
            "session-1", "Test Bank", "GR", null,
            [new BankingAccountDto("uid-1", "GR16011", "Main", "XYZ", "CACC")]);

        await sut.Handler.Handle(new CompleteBankingConnectionCommand(business.Id, Code));

        sut.Accounts.Store.Values.Single().Currency.Should().Be(Currency.EUR);
    }

    [Fact]
    public async Task WritesAnAuditEntryOnConfirmation()
    {
        var sut = BuildSut();
        var business = SeedBusiness(sut);

        await sut.Handler.Handle(new CompleteBankingConnectionCommand(business.Id, Code));

        sut.Audit.Calls.Should().ContainSingle()
            .Which.Action.Should().Be(AuditAction.BankingConnectionConfirmed);
    }

    [Fact]
    public async Task NeverPutsTheSessionIdInTheAuditDetails()
    {
        var sut = BuildSut();
        var business = SeedBusiness(sut);

        await sut.Handler.Handle(new CompleteBankingConnectionCommand(business.Id, Code));

        System.Text.Json.JsonSerializer.Serialize(sut.Audit.Calls.Single().Details)
            .Should().NotContain("session-1");
    }

    [Fact]
    public async Task ReturnsBusinessNotFoundForABusinessOutsideTheCurrentTenant()
    {
        var sut = BuildSut();

        // The repository fake mirrors the real tenant query filter by simply not
        // holding the row — an id smuggled in through `state` resolves to null.
        var result = await sut.Handler.Handle(new CompleteBankingConnectionCommand(Guid.NewGuid(), Code));

        result.Should().BeOfType<CompleteBankingConnectionResult.BusinessNotFound>();
        sut.Client.CompleteCalls.Should().BeEmpty();
    }

    [Fact]
    public async Task ReturnsInvalidCodeForABlankCode()
    {
        var sut = BuildSut();
        var business = SeedBusiness(sut);

        var result = await sut.Handler.Handle(new CompleteBankingConnectionCommand(business.Id, "  "));

        result.Should().BeOfType<CompleteBankingConnectionResult.InvalidCode>();
        sut.Client.CompleteCalls.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(CodeFailures))]
    public async Task ReturnsInvalidCodeWhenTheAggregatorRejectsTheExchange(BankingSessionResult failure)
    {
        var sut = BuildSut();
        var business = SeedBusiness(sut);
        sut.Client.NextSession = failure;

        var result = await sut.Handler.Handle(new CompleteBankingConnectionCommand(business.Id, Code));

        result.Should().BeOfType<CompleteBankingConnectionResult.InvalidCode>();
        sut.Credentials.Store.Should().BeEmpty();
    }

    public static TheoryData<BankingSessionResult> CodeFailures() =>
    [
        new BankingSessionResult.InvalidCode(),
        new BankingSessionResult.SessionExpired(),
    ];

    [Fact]
    public async Task RefusesASessionThatGrantedNoAccounts()
    {
        var sut = BuildSut();
        var business = SeedBusiness(sut);
        sut.Client.NextSession = new BankingSessionResult.Success("session-1", "Test Bank", "GR", null, []);

        var result = await sut.Handler.Handle(new CompleteBankingConnectionCommand(business.Id, Code));

        result.Should().BeOfType<CompleteBankingConnectionResult.NoAccountsGranted>();
        sut.Credentials.Store.Should().BeEmpty("a connection with no accounts would look live but never sync");
    }

    [Theory]
    [MemberData(nameof(TransportFailures))]
    public async Task MapsTransportFailuresToBankingUnavailable(BankingSessionResult failure)
    {
        var sut = BuildSut();
        var business = SeedBusiness(sut);
        sut.Client.NextSession = failure;

        var result = await sut.Handler.Handle(new CompleteBankingConnectionCommand(business.Id, Code));

        result.Should().BeOfType<CompleteBankingConnectionResult.BankingUnavailable>();
        sut.Credentials.Store.Should().BeEmpty();
    }

    public static TheoryData<BankingSessionResult> TransportFailures() =>
    [
        new BankingSessionResult.Unauthorized("bad jwt"),
        new BankingSessionResult.NetworkError("timeout"),
        new BankingSessionResult.BankingServerError(503, "unavailable"),
    ];
}
