namespace Roivo.Application.Features.Banking.Queries.ListBankingProviders;

/// <summary>Lists the banks the user can connect to, for the bank picker.</summary>
public sealed record ListBankingProvidersQuery;

public abstract record ListBankingProvidersResult
{
    public sealed record Success(IReadOnlyList<BankingProviderSummary> Providers) : ListBankingProvidersResult;
    public sealed record BankingUnavailable(string Message) : ListBankingProvidersResult;
}

public sealed record BankingProviderSummary(string Name, string? LogoUrl);
