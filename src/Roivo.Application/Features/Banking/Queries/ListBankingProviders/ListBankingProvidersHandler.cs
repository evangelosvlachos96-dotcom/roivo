using Roivo.Application.Abstractions.Banking;
using Roivo.Application.Abstractions.Banking.Results;

namespace Roivo.Application.Features.Banking.Queries.ListBankingProviders;

public sealed class ListBankingProvidersHandler
{
    private readonly IBankingClient _banking;
    private readonly IBankingConnectionOptions _options;

    public ListBankingProvidersHandler(IBankingClient banking, IBankingConnectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(banking);
        ArgumentNullException.ThrowIfNull(options);

        _banking = banking;
        _options = options;
    }

    public async Task<ListBankingProvidersResult> Handle(
        ListBankingProvidersQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var result = await _banking.ListProvidersAsync(_options.Country, cancellationToken).ConfigureAwait(false);

        return result switch
        {
            BankingProvidersResult.Success success => new ListBankingProvidersResult.Success(
                success.Providers
                    // Business access only — Roivo has nothing to say about a
                    // sole trader's personal current account. A bank that
                    // declares no psu_types is kept rather than hidden.
                    .Where(p => p.PsuTypes.Count == 0
                             || p.PsuTypes.Any(t => string.Equals(t, "business", StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(p => new BankingProviderSummary(p.Name, p.LogoUrl))
                    .ToList()),
            BankingProvidersResult.Unauthorized u => new ListBankingProvidersResult.BankingUnavailable(u.Message),
            BankingProvidersResult.NetworkError ne => new ListBankingProvidersResult.BankingUnavailable(ne.Message),
            BankingProvidersResult.BankingServerError se => new ListBankingProvidersResult.BankingUnavailable($"Banking API returned {se.StatusCode}: {se.Message}"),
            _ => new ListBankingProvidersResult.BankingUnavailable("Unrecognised banking API response."),
        };
    }
}
