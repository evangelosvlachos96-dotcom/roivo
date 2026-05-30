using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Auditing;
using Roivo.Core.Domain.Businesses;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Exceptions;

namespace Roivo.Application.Features.Businesses.Commands.ReactivateBusiness;

public sealed class ReactivateBusinessHandler
{
    private readonly IBusinessRepository _repository;
    private readonly IAuditWriter _audit;
    private readonly ITenantContext _tenant;

    public ReactivateBusinessHandler(
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

    public async Task<ReactivateBusinessResult> Handle(ReactivateBusinessCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!BusinessPermissions.CanReactivate(_tenant.CurrentTenantType))
            return new ReactivateBusinessResult.Forbidden("Δεν επιτρέπεται η επανενεργοποίηση επιχείρησης από αυτόν τον τύπο λογαριασμού.");

        var entity = await _repository.GetByIdAsync(command.Id, cancellationToken);
        if (entity is null)
            return new ReactivateBusinessResult.NotFound();

        if (await _repository.AfmExistsAsync(entity.Afm, activeOnly: true, excludeId: entity.Id, cancellationToken))
            return new ReactivateBusinessResult.ConflictsWithActive(entity.Afm);

        try
        {
            entity.Reactivate();
        }
        catch (DomainException)
        {
            return new ReactivateBusinessResult.AlreadyActive();
        }

        // Reactivation restores the prior state exactly — no name/address
        // overwrite. The user edits via the normal update flow if they want.
        await _repository.UpdateAsync(entity, cancellationToken);

        await _audit.WriteAsync(
            action: AuditAction.BusinessReactivated,
            tenantId: _tenant.CurrentTenantId,
            entityType: nameof(Business),
            entityId: entity.Id.ToString(),
            details: new { entity.Name, entity.Afm },
            cancellationToken: cancellationToken);

        return new ReactivateBusinessResult.Success();
    }
}
