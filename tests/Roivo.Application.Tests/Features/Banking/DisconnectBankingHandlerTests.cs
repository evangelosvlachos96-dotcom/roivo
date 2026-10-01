using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Roivo.Application.Abstractions.Banking.Results;
using Roivo.Application.Features.Banking.Commands.DisconnectBanking;
using Roivo.Application.Tests.Fakes;
using Roivo.Core.Domain.Auditing;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Tests.Features.Banking;

public class DisconnectBankingHandlerTests
{
    private const string ValidAfm = "094014201";

    private sealed record Sut(
        DisconnectBankingHandler Handler,
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
            new DisconnectBankingHandler(
                businesses, accounts, client, credentials, audit, tenant,
                NullLogger<DisconnectBankingHandler>.Instance),
            businesses, accounts, client, credentials, audit, tenant);
    }

    private static Business SeedConnected(Sut sut)
    {
        var business = Business.Create("Acme", ValidAfm, null, null);
        business.TenantId = sut.Tenant.CurrentTenantId;
        business.RecordBankingConnection("Test Bank", DateTime.UtcNow.AddDays(90));
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

        return business;
    }

    [Fact]
    public async Task ClearsTheSessionAccountsAndConnectionMetadata()
    {
        var sut = BuildSut();
        var business = SeedConnected(sut);

        var result = await sut.Handler.Handle(new DisconnectBankingCommand(business.Id));

        result.Should().BeOfType<DisconnectBankingResult.Success>();
        sut.Credentials.Store.Should().BeEmpty();
        sut.Accounts.Store.Should().BeEmpty();
        business.BankingProviderName.Should().BeNull();
        business.BankingConsentExpiresAt.Should().BeNull();
        business.LastBankingSyncAt.Should().BeNull();
    }

    [Fact]
    public async Task RevokesTheConsentAtTheAggregator()
    {
        var sut = BuildSut();
        var business = SeedConnected(sut);

        await sut.Handler.Handle(new DisconnectBankingCommand(business.Id));

        sut.Client.RevokeCalls.Should().ContainSingle().Which.Should().Be("session-1");
    }

    [Fact]
    public async Task StillDisconnectsLocallyWhenRevocationFails()
    {
        var sut = BuildSut();
        var business = SeedConnected(sut);
        sut.Client.NextRevoke = new BankingRevokeResult.Failed("aggregator down");

        var result = await sut.Handler.Handle(new DisconnectBankingCommand(business.Id));

        result.Should().BeOfType<DisconnectBankingResult.Success>();
        sut.Credentials.Store.Should().BeEmpty();
    }

    [Fact]
    public async Task ClearsAFailureStreakSoAReconnectStartsClean()
    {
        var sut = BuildSut();
        var business = SeedConnected(sut);
        business.RecordBankingSyncFailure("SessionExpired");

        await sut.Handler.Handle(new DisconnectBankingCommand(business.Id));

        business.BankingSyncErrorCount.Should().Be(0);
        business.BankingFirstFailureAt.Should().BeNull();
    }

    [Fact]
    public async Task WritesAnAuditEntry()
    {
        var sut = BuildSut();
        var business = SeedConnected(sut);

        await sut.Handler.Handle(new DisconnectBankingCommand(business.Id));

        sut.Audit.Calls.Should().ContainSingle()
            .Which.Action.Should().Be(AuditAction.BankingConnectionRevoked);
    }

    [Fact]
    public async Task SkipsRevocationWhenNoSessionIsStored()
    {
        var sut = BuildSut();
        var business = SeedConnected(sut);
        sut.Credentials.Store.Clear();

        var result = await sut.Handler.Handle(new DisconnectBankingCommand(business.Id));

        result.Should().BeOfType<DisconnectBankingResult.Success>();
        sut.Client.RevokeCalls.Should().BeEmpty();
    }

    [Fact]
    public async Task ReturnsBusinessNotFoundForAnUnknownBusiness()
    {
        var sut = BuildSut();

        var result = await sut.Handler.Handle(new DisconnectBankingCommand(Guid.NewGuid()));

        result.Should().BeOfType<DisconnectBankingResult.BusinessNotFound>();
    }
}
