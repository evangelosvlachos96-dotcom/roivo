using FluentAssertions;
using Roivo.Application.Abstractions.Banking.Results;
using Roivo.Application.Features.Banking.Queries.ListBankingProviders;
using Roivo.Application.Tests.Fakes;

namespace Roivo.Application.Tests.Features.Banking;

public class ListBankingProvidersHandlerTests
{
    private sealed record Sut(
        ListBankingProvidersHandler Handler,
        FakeBankingClient Client,
        FakeBankingConnectionOptions Options);

    private static Sut BuildSut()
    {
        var client = new FakeBankingClient();
        var options = new FakeBankingConnectionOptions();
        return new Sut(new ListBankingProvidersHandler(client, options), client, options);
    }

    [Fact]
    public async Task AsksForTheConfiguredCountry()
    {
        var sut = BuildSut();
        sut.Options.Country = "GR";

        await sut.Handler.Handle(new ListBankingProvidersQuery());

        sut.Client.ProviderCalls.Should().ContainSingle().Which.Should().Be("GR");
    }

    [Fact]
    public async Task KeepsOnlyBanksThatOfferBusinessAccess()
    {
        var sut = BuildSut();
        sut.Client.NextProviders = new BankingProvidersResult.Success(
        [
            new BankingProviderDto("Business Bank", "GR", null, ["business"]),
            new BankingProviderDto("Personal Only", "GR", null, ["personal"]),
        ]);

        var result = await sut.Handler.Handle(new ListBankingProvidersQuery());

        result.Should().BeOfType<ListBankingProvidersResult.Success>()
            .Which.Providers.Select(p => p.Name).Should().Equal("Business Bank");
    }

    [Fact]
    public async Task KeepsBanksThatDeclareNoPsuTypesRatherThanHidingThem()
    {
        var sut = BuildSut();
        sut.Client.NextProviders = new BankingProvidersResult.Success(
            [new BankingProviderDto("Quiet Bank", "GR", null, [])]);

        var result = await sut.Handler.Handle(new ListBankingProvidersQuery());

        result.Should().BeOfType<ListBankingProvidersResult.Success>()
            .Which.Providers.Should().ContainSingle();
    }

    [Fact]
    public async Task SortsBanksByNameForThePicker()
    {
        var sut = BuildSut();
        sut.Client.NextProviders = new BankingProvidersResult.Success(
        [
            new BankingProviderDto("Piraeus", "GR", null, ["business"]),
            new BankingProviderDto("Alpha", "GR", null, ["business"]),
            new BankingProviderDto("Eurobank", "GR", null, ["business"]),
        ]);

        var result = await sut.Handler.Handle(new ListBankingProvidersQuery());

        result.Should().BeOfType<ListBankingProvidersResult.Success>()
            .Which.Providers.Select(p => p.Name).Should().Equal("Alpha", "Eurobank", "Piraeus");
    }

    [Theory]
    [MemberData(nameof(Failures))]
    public async Task MapsEveryFailureToBankingUnavailable(BankingProvidersResult failure)
    {
        var sut = BuildSut();
        sut.Client.NextProviders = failure;

        var result = await sut.Handler.Handle(new ListBankingProvidersQuery());

        result.Should().BeOfType<ListBankingProvidersResult.BankingUnavailable>();
    }

    public static TheoryData<BankingProvidersResult> Failures() =>
    [
        new BankingProvidersResult.Unauthorized("Application does not exist"),
        new BankingProvidersResult.NetworkError("dns"),
        new BankingProvidersResult.BankingServerError(500, "boom"),
    ];
}
