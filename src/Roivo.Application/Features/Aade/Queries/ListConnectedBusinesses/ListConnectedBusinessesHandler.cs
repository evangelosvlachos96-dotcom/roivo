using Roivo.Application.Abstractions;

namespace Roivo.Application.Features.Aade.Queries.ListConnectedBusinesses;

public sealed class ListConnectedBusinessesHandler
{
    private readonly IBusinessRepository _businesses;

    public ListConnectedBusinessesHandler(IBusinessRepository businesses)
    {
        ArgumentNullException.ThrowIfNull(businesses);
        _businesses = businesses;
    }

    public Task<IReadOnlyList<Guid>> Handle(ListConnectedBusinessesQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return _businesses.ListIdsWithAadeCredentialsAcrossAllTenantsAsync(cancellationToken);
    }
}
