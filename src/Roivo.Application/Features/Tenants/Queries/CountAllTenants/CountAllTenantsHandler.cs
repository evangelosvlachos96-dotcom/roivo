using Roivo.Application.Abstractions;

namespace Roivo.Application.Features.Tenants.Queries.CountAllTenants;

public sealed class CountAllTenantsHandler
{
    private readonly ITenantRepository _repository;

    public CountAllTenantsHandler(ITenantRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public Task<int> Handle(CountAllTenantsQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return _repository.CountAllAsync(cancellationToken);
    }
}
