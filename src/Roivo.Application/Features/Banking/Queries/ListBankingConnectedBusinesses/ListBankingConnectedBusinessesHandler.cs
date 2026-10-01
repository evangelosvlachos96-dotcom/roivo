using Roivo.Application.Abstractions;

namespace Roivo.Application.Features.Banking.Queries.ListBankingConnectedBusinesses;

public sealed class ListBankingConnectedBusinessesHandler
{
    private readonly IBusinessRepository _businesses;

    public ListBankingConnectedBusinessesHandler(IBusinessRepository businesses)
    {
        ArgumentNullException.ThrowIfNull(businesses);
        _businesses = businesses;
    }

    public Task<IReadOnlyList<Guid>> Handle(
        ListBankingConnectedBusinessesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return _businesses.ListIdsWithBankingCredentialsAcrossAllTenantsAsync(cancellationToken);
    }
}
