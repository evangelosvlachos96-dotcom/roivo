using FluentAssertions;
using Roivo.Application.Abstractions.Banking.Results;
using Roivo.Application.Features.Banking.Commands.SyncBusinessBankTransactions;
using Roivo.Application.Tests.Fakes;
using Roivo.Core.Domain.Auditing;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Tests.Features.Banking;

public class SyncBusinessBankTransactionsHandlerTests
{
    private const string ValidAfm = "094014201";

    private sealed record Sut(
        SyncBusinessBankTransactionsHandler Handler,
        FakeBusinessRepository Businesses,
        FakeBankAccountRepository Accounts,
        FakeBankTransactionRepository Transactions,
        FakeBankingClient Client,
        FakeBankingCredentialStore Credentials,
        FakeBankingConnectionOptions Options,
        FakeAuditWriter Audit,
        FakeTenantContext Tenant);

    private static Sut BuildSut()
    {
        var businesses = new FakeBusinessRepository();
        var accounts = new FakeBankAccountRepository();
        var transactions = new FakeBankTransactionRepository();
        var client = new FakeBankingClient();
        var credentials = new FakeBankingCredentialStore();
        var options = new FakeBankingConnectionOptions();
        var audit = new FakeAuditWriter();
        var tenant = new FakeTenantContext();

        return new Sut(
            new SyncBusinessBankTransactionsHandler(businesses, accounts, transactions, client, credentials, options, audit),
            businesses, accounts, transactions, client, credentials, options, audit, tenant);
    }

    /// <summary>Seeds a connected business with one syncable account.</summary>
    private static (Business Business, BankAccount Account) SeedConnected(Sut sut)
    {
        var business = Business.Create("Acme", ValidAfm, null, null);
        business.TenantId = sut.Tenant.CurrentTenantId;
        sut.Businesses.Store[business.Id] = business;
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

        return (business, account);
    }

    private static BankingTransactionDto Transaction(string externalId, decimal amount = 100m) =>
        new(
            ExternalId: externalId,
            BookingDate: new DateOnly(2026, 9, 1),
            ValueDate: new DateOnly(2026, 9, 1),
            Amount: amount,
            Currency: "EUR",
            CounterpartyName: "Supplier",
            CounterpartyIban: "GR9999",
            Reference: "INV-1",
            RawPayload: "{}");

    [Fact]
    public async Task StoresFetchedTransactionsAndReportsTheCounts()
    {
        var sut = BuildSut();
        var (business, account) = SeedConnected(sut);
        sut.Client.NextFetch = new BankingFetchResult.Success([Transaction("t1"), Transaction("t2")]);

        var result = await sut.Handler.Handle(new SyncBusinessBankTransactionsCommand(business.Id));

        var success = result.Should().BeOfType<SyncBusinessBankTransactionsResult.Success>().Subject;
        success.NewCount.Should().Be(2);
        success.UpdatedCount.Should().Be(0);
        success.AccountCount.Should().Be(1);
        sut.Transactions.Store.Should().HaveCount(2);
        sut.Transactions.Store.Values.Should().OnlyContain(t => t.BankAccountId == account.Id);
    }

    [Fact]
    public async Task SetsTenantIdExplicitlyBecauseTheCronHasNoAmbientTenant()
    {
        var sut = BuildSut();
        var (business, _) = SeedConnected(sut);
        sut.Client.NextFetch = new BankingFetchResult.Success([Transaction("t1")]);

        await sut.Handler.Handle(new SyncBusinessBankTransactionsCommand(business.Id));

        sut.Transactions.Store.Values.Single().TenantId.Should().Be(business.TenantId);
    }

    [Fact]
    public async Task ReRunningOverTheSameWindowUpdatesRatherThanDuplicates()
    {
        var sut = BuildSut();
        var (business, _) = SeedConnected(sut);
        sut.Client.NextFetch = new BankingFetchResult.Success([Transaction("t1")]);

        await sut.Handler.Handle(new SyncBusinessBankTransactionsCommand(business.Id));
        var second = await sut.Handler.Handle(new SyncBusinessBankTransactionsCommand(business.Id));

        second.Should().BeOfType<SyncBusinessBankTransactionsResult.Success>()
            .Which.UpdatedCount.Should().Be(1);
        sut.Transactions.Store.Should().ContainSingle();
    }

    [Fact]
    public async Task FetchesTheConfiguredLookbackWindow()
    {
        var sut = BuildSut();
        sut.Options.SyncLookbackDays = 30;
        var (business, _) = SeedConnected(sut);

        await sut.Handler.Handle(new SyncBusinessBankTransactionsCommand(business.Id));

        var call = sut.Client.FetchCalls.Should().ContainSingle().Subject;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        call.To.Should().Be(today);
        call.From.Should().Be(today.AddDays(-30));
    }

    [Fact]
    public async Task FetchesEveryAccountOfTheBusiness()
    {
        var sut = BuildSut();
        var (business, _) = SeedConnected(sut);
        var second = new BankAccount
        {
            TenantId = business.TenantId,
            BusinessId = business.Id,
            ExternalAccountUid = "uid-2",
            BankName = "Test Bank",
            Iban = "GR2202",
        };
        sut.Accounts.Store[second.Id] = second;

        await sut.Handler.Handle(new SyncBusinessBankTransactionsCommand(business.Id));

        sut.Client.FetchCalls.Select(c => c.AccountUid).Should().BeEquivalentTo(["uid-1", "uid-2"]);
    }

    [Fact]
    public async Task StampsLastSyncAndClearsAnyFailureStreakOnSuccess()
    {
        var sut = BuildSut();
        var (business, _) = SeedConnected(sut);
        business.RecordBankingSyncFailure("NetworkError");

        await sut.Handler.Handle(new SyncBusinessBankTransactionsCommand(business.Id));

        business.LastBankingSyncAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        business.BankingSyncErrorCount.Should().Be(0);
        business.BankingLastFailureReason.Should().BeNull();
    }

    [Fact]
    public async Task WritesAnAuditEntryOnSuccess()
    {
        var sut = BuildSut();
        var (business, _) = SeedConnected(sut);

        await sut.Handler.Handle(new SyncBusinessBankTransactionsCommand(business.Id));

        sut.Audit.Calls.Should().ContainSingle()
            .Which.Action.Should().Be(AuditAction.BankingSyncCompleted);
    }

    [Fact]
    public async Task PreservesTheSignTheClientAssigned()
    {
        var sut = BuildSut();
        var (business, _) = SeedConnected(sut);
        sut.Client.NextFetch = new BankingFetchResult.Success([Transaction("t1", -42.50m)]);

        await sut.Handler.Handle(new SyncBusinessBankTransactionsCommand(business.Id));

        var stored = sut.Transactions.Store.Values.Single();
        stored.Amount.Should().Be(-42.50m);
        stored.Currency.Should().Be(Currency.EUR);
    }

    [Fact]
    public async Task ReturnsBusinessNotFoundForAnUnknownBusiness()
    {
        var sut = BuildSut();

        var result = await sut.Handler.Handle(new SyncBusinessBankTransactionsCommand(Guid.NewGuid()));

        result.Should().BeOfType<SyncBusinessBankTransactionsResult.BusinessNotFound>();
    }

    [Fact]
    public async Task TheCronCanReachBusinessesDespiteHavingNoTenantScope()
    {
        var sut = BuildSut();
        var (business, _) = SeedConnected(sut);
        sut.Businesses.SimulateNoTenantScope = true;
        sut.Client.NextFetch = new BankingFetchResult.Success([Transaction("t1")]);

        var result = await sut.Handler.Handle(
            new SyncBusinessBankTransactionsCommand(business.Id, BypassTenantScope: true));

        result.Should().BeOfType<SyncBusinessBankTransactionsResult.Success>();
    }

    [Fact]
    public async Task ARequestScopedCallerStillCannotReachAnotherTenantsBusiness()
    {
        var sut = BuildSut();
        var (business, _) = SeedConnected(sut);
        sut.Businesses.SimulateNoTenantScope = true;

        var result = await sut.Handler.Handle(new SyncBusinessBankTransactionsCommand(business.Id));

        result.Should().BeOfType<SyncBusinessBankTransactionsResult.BusinessNotFound>();
        sut.Client.FetchCalls.Should().BeEmpty();
    }

    [Fact]
    public async Task ReturnsNotConnectedWhenNoSessionIsStored()
    {
        var sut = BuildSut();
        var (business, _) = SeedConnected(sut);
        sut.Credentials.Store.Clear();

        var result = await sut.Handler.Handle(new SyncBusinessBankTransactionsCommand(business.Id));

        result.Should().BeOfType<SyncBusinessBankTransactionsResult.NotConnected>();
        sut.Client.FetchCalls.Should().BeEmpty();
    }

    [Fact]
    public async Task ReturnsNotConnectedWhenNoAccountHasAnAggregatorHandle()
    {
        var sut = BuildSut();
        var (business, account) = SeedConnected(sut);
        account.ExternalAccountUid = null;

        var result = await sut.Handler.Handle(new SyncBusinessBankTransactionsCommand(business.Id));

        result.Should().BeOfType<SyncBusinessBankTransactionsResult.NotConnected>();
    }

    [Fact]
    public async Task RecordsAFailureStreakAndAuditsWhenTheConsentHasLapsed()
    {
        var sut = BuildSut();
        var (business, _) = SeedConnected(sut);
        sut.Client.NextFetch = new BankingFetchResult.SessionExpired();

        var result = await sut.Handler.Handle(new SyncBusinessBankTransactionsCommand(business.Id));

        result.Should().BeOfType<SyncBusinessBankTransactionsResult.SessionExpired>();
        business.BankingSyncErrorCount.Should().Be(1);
        business.BankingLastFailureReason.Should().Be("SessionExpired");
        sut.Audit.Calls.Should().ContainSingle().Which.Action.Should().Be(AuditAction.BankingSyncFailed);
    }

    [Fact]
    public async Task RepeatedFailuresAccumulateButKeepTheOriginalStartTime()
    {
        var sut = BuildSut();
        var (business, _) = SeedConnected(sut);
        sut.Client.NextFetch = new BankingFetchResult.NetworkError("timeout");

        await sut.Handler.Handle(new SyncBusinessBankTransactionsCommand(business.Id));
        var firstFailureAt = business.BankingFirstFailureAt;
        await sut.Handler.Handle(new SyncBusinessBankTransactionsCommand(business.Id));

        business.BankingSyncErrorCount.Should().Be(2);
        business.BankingFirstFailureAt.Should().Be(firstFailureAt);
    }

    [Fact]
    public async Task DoesNotStampLastSyncWhenAFetchFails()
    {
        var sut = BuildSut();
        var (business, _) = SeedConnected(sut);
        sut.Client.NextFetch = new BankingFetchResult.BankingServerError(500, "boom");

        var result = await sut.Handler.Handle(new SyncBusinessBankTransactionsCommand(business.Id));

        result.Should().BeOfType<SyncBusinessBankTransactionsResult.BankingServerError>();
        business.LastBankingSyncAt.Should().BeNull("a partial sync must not look like a complete one");
        sut.Transactions.Store.Should().BeEmpty();
    }

    [Fact]
    public async Task MapsAnUnauthorizedFetchToAServerErrorAndRecordsIt()
    {
        var sut = BuildSut();
        var (business, _) = SeedConnected(sut);
        sut.Client.NextFetch = new BankingFetchResult.Unauthorized("nope");

        var result = await sut.Handler.Handle(new SyncBusinessBankTransactionsCommand(business.Id));

        result.Should().BeOfType<SyncBusinessBankTransactionsResult.BankingServerError>()
            .Which.StatusCode.Should().Be(401);
        business.BankingLastFailureReason.Should().Be("Unauthorized");
    }
}
