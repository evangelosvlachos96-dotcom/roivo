using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;

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

        var entity = await _repository.GetByIdAsync(command.Id, cancellationToken);
        if (entity is null)
            return new ReactivateBusinessResult.NotFound();

        if (entity.IsActive)
            return new ReactivateBusinessResult.AlreadyActive();

        // Guard against the race where another active business already holds
        // this AFM (e.g., a parallel create completed between deactivation
        // and reactivation).
        if (await _repository.AfmExistsAsync(entity.Afm, activeOnly: true, excludeId: entity.Id, cancellationToken))
            return new ReactivateBusinessResult.ConflictsWithActive(entity.Afm);

        entity.IsActive = true;
        entity.Name = (command.Name ?? string.Empty).Trim();
        entity.Kad = string.IsNullOrWhiteSpace(command.Kad) ? null : command.Kad.Trim();
        entity.Address = string.IsNullOrWhiteSpace(command.Address) ? null : command.Address.Trim();

        await _repository.UpdateAsync(entity, cancellationToken);

        await _audit.WriteAsync(
            action: "BusinessReactivated",
            tenantId: _tenant.CurrentTenantId,
            entityType: nameof(Business),
            entityId: entity.Id.ToString(),
            details: new { entity.Name, entity.Afm },
            cancellationToken: cancellationToken);

        return new ReactivateBusinessResult.Success();
    }
}
