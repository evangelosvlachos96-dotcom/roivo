using FluentAssertions;
using Roivo.Application.Abstractions.Banking.Results;
using Roivo.Application.Features.Banking.Commands.ConnectBanking;
using Roivo.Application.Tests.Fakes;
using Roivo.Core.Domain.Auditing;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Tests.Features.Banking;

public class ConnectBankingHandlerTests
{
    private const string ValidAfm = "094014201";
    private const string BankName = "Test Bank";

    private sealed record Sut(
        ConnectBankingHandler Handler,
        FakeBusinessRepository Businesses,
        FakeBankingClient Client,
        FakeBankingCredentialStore Credentials,
        FakeBankingConnectionOptions Options,
        FakeAuditWriter Audit,
        FakeTenantContext Tenant);

    private static Sut BuildSut()
    {
        var businesses = new FakeBusinessRepository();
        var client = new FakeBankingClient();
        var credentials = new FakeBankingCredentialStore();
        var options = new FakeBankingConnectionOptions();
        var audit = new FakeAuditWriter();
        var tenant = new FakeTenantContext();

        return new Sut(
            new ConnectBankingHandler(businesses, client, credentials, options, audit, tenant),
            businesses, client, credentials, options, audit, tenant);
    }

    private static Business SeedBusiness(Sut sut)
    {
        var business = Business.Create("Acme", ValidAfm, null, null);
        business.TenantId = sut.Tenant.CurrentTenantId;
        sut.Businesses.Store[business.Id] = business;
        return business;
    }

    [Fact]
    public async Task ReturnsTheAuthorizationUrlOnTheHappyPath()
    {
        var sut = BuildSut();
        var business = SeedBusiness(sut);

        var result = await sut.Handler.Handle(new ConnectBankingCommand(business.Id, BankName));

        result.Should().BeOfType<ConnectBankingResult.Success>()
            .Which.AuthorizationUrl.Should().Be("https://auth.example/consent?id=1");
    }

    [Fact]
    public async Task PassesTheConfiguredConnectionParametersToTheClient()
    {
        var sut = BuildSut();
        sut.Options.Country = "GR";
        sut.Options.ConsentValidDays = 90;
        var business = SeedBusiness(sut);

        await sut.Handler.Handle(new ConnectBankingCommand(business.Id, BankName));

        var request = sut.Client.AuthorizationCalls.Should().ContainSingle().Subject;
        request.BankName.Should().Be(BankName);
        request.Country.Should().Be("GR");
        request.RedirectUrl.Should().Be(sut.Options.RedirectUrl);
        request.AccessValidUntil.Should().BeCloseTo(DateTimeOffset.UtcNow.AddDays(90), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task CarriesTheBusinessIdInStateSoTheCallbackCanRouteBack()
    {
        var sut = BuildSut();
        var business = SeedBusiness(sut);

        await sut.Handler.Handle(new ConnectBankingCommand(business.Id, BankName));

        sut.Client.AuthorizationCalls.Single().State.Should().Be(business.Id.ToString("N"));
    }

    [Fact]
    public async Task WritesAnAuditEntryOnInitiation()
    {
        var sut = BuildSut();
        var business = SeedBusiness(sut);

        await sut.Handler.Handle(new ConnectBankingCommand(business.Id, BankName));

        sut.Audit.Calls.Should().ContainSingle()
            .Which.Action.Should().Be(AuditAction.BankingConnectionInitiated);
    }

    [Fact]
    public async Task StoresNothingUntilTheUserComesBackWithACode()
    {
        var sut = BuildSut();
        var business = SeedBusiness(sut);

        await sut.Handler.Handle(new ConnectBankingCommand(business.Id, BankName));

        sut.Credentials.Store.Should().BeEmpty();
        business.BankingProviderName.Should().BeNull();
    }

    [Fact]
    public async Task ReturnsBusinessNotFoundForAnUnknownBusiness()
    {
        var sut = BuildSut();

        var result = await sut.Handler.Handle(new ConnectBankingCommand(Guid.NewGuid(), BankName));

        result.Should().BeOfType<ConnectBankingResult.BusinessNotFound>();
    }

    [Fact]
    public async Task ReturnsBusinessNotFoundForAnInactiveBusiness()
    {
        var sut = BuildSut();
        var business = SeedBusiness(sut);
        business.Deactivate();

        var result = await sut.Handler.Handle(new ConnectBankingCommand(business.Id, BankName));

        result.Should().BeOfType<ConnectBankingResult.BusinessNotFound>();
    }

    [Fact]
    public async Task ReturnsAlreadyConnectedWhenASessionIsStored()
    {
        var sut = BuildSut();
        var business = SeedBusiness(sut);
        sut.Credentials.Store[business.Id] = "session-1";

        var result = await sut.Handler.Handle(new ConnectBankingCommand(business.Id, BankName));

        result.Should().BeOfType<ConnectBankingResult.AlreadyConnected>();
        sut.Client.AuthorizationCalls.Should().BeEmpty();
    }

    [Fact]
    public async Task ReturnsUnknownProviderWhenNoBankWasChosen()
    {
        var sut = BuildSut();
        var business = SeedBusiness(sut);

        var result = await sut.Handler.Handle(new ConnectBankingCommand(business.Id, "  "));

        result.Should().BeOfType<ConnectBankingResult.UnknownProvider>();
        sut.Client.AuthorizationCalls.Should().BeEmpty();
    }

    [Fact]
    public async Task ReturnsUnknownProviderWhenTheAggregatorRejectsTheBank()
    {
        var sut = BuildSut();
        var business = SeedBusiness(sut);
        sut.Client.NextAuthorization = new BankingAuthorizationResult.UnknownProvider(BankName, "GR");

        var result = await sut.Handler.Handle(new ConnectBankingCommand(business.Id, BankName));

        result.Should().BeOfType<ConnectBankingResult.UnknownProvider>();
    }

    [Theory]
    [MemberData(nameof(TransportFailures))]
    public async Task MapsTransportFailuresToBankingUnavailable(BankingAuthorizationResult failure)
    {
        var sut = BuildSut();
        var business = SeedBusiness(sut);
        sut.Client.NextAuthorization = failure;

        var result = await sut.Handler.Handle(new ConnectBankingCommand(business.Id, BankName));

        result.Should().BeOfType<ConnectBankingResult.BankingUnavailable>();
        sut.Audit.Calls.Should().BeEmpty("a failed initiation is not an initiation");
    }

    public static TheoryData<BankingAuthorizationResult> TransportFailures() =>
    [
        new BankingAuthorizationResult.Unauthorized("bad jwt"),
        new BankingAuthorizationResult.NetworkError("timeout"),
        new BankingAuthorizationResult.BankingServerError(502, "bad gateway"),
    ];
}
