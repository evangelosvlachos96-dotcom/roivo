using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Features.Tenants.Queries.GetCurrentTenant;

public sealed class GetCurrentTenantHandler
{
    private readonly ITenantRepository _repository;
    private readonly ITenantContext _tenant;

    public GetCurrentTenantHandler(ITenantRepository repository, ITenantContext tenant)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(tenant);

        _repository = repository;
        _tenant = tenant;
    }

    public Task<Tenant?> Handle(GetCurrentTenantQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var id = _tenant.CurrentTenantId;
        if (id == Guid.Empty)
            return Task.FromResult<Tenant?>(null);

        return _repository.GetByIdAsync(id, cancellationToken);
    }
}
