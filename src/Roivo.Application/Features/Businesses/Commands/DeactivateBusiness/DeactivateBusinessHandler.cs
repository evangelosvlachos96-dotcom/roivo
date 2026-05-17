using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Features.Businesses.Commands.DeactivateBusiness;

public sealed class DeactivateBusinessHandler
{
    private readonly IBusinessRepository _repository;
    private readonly IAuditWriter _audit;
    private readonly ITenantContext _tenant;

    public DeactivateBusinessHandler(
        IBusinessRepository repository,
        IAuditWriter audit,
        ITenantContext tenant)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(audit);
        ArgumentNullException.ThrowIfNull(tenant);

        _repository = repository;
        _audit = audit;
        _tenant = tenant;
    }

    public async Task<DeactivateBusinessResult> Handle(DeactivateBusinessCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var entity = await _repository.GetByIdAsync(command.Id, cancellationToken);
        if (entity is null)
            return new DeactivateBusinessResult.NotFound();

        if (!entity.IsActive)
            return new DeactivateBusinessResult.AlreadyInactive();

        entity.IsActive = false;
        await _repository.UpdateAsync(entity, cancellationToken);

        await _audit.WriteAsync(
            action: "BusinessDeactivated",
            tenantId: _tenant.CurrentTenantId,
            entityType: nameof(Business),
            entityId: entity.Id.ToString(),
            details: new { entity.Name, entity.Afm },
            cancellationToken: cancellationToken);

        return new DeactivateBusinessResult.Success();
    }
}
