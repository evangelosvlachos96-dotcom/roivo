using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Businesses;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Exceptions;
using Roivo.Core.Domain.Validation;

namespace Roivo.Application.Features.Businesses.Commands.UpdateBusiness;

public sealed class UpdateBusinessHandler
{
    private readonly IBusinessRepository _repository;
    private readonly IAuditWriter _audit;
    private readonly ITenantContext _tenant;

    public UpdateBusinessHandler(
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

    public async Task<UpdateBusinessResult> Handle(UpdateBusinessCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var entity = await _repository.GetByIdActiveOnlyAsync(command.Id, cancellationToken);
        if (entity is null)
            return new UpdateBusinessResult.NotFound();

        var newAfm = (command.Afm ?? string.Empty).Trim();
        var newName = (command.Name ?? string.Empty).Trim();
        var newKad = string.IsNullOrWhiteSpace(command.Kad) ? null : command.Kad.Trim();
        var newAddress = string.IsNullOrWhiteSpace(command.Address) ? null : command.Address.Trim();

        // Validate upfront before any entity mutation so a later failure
        // doesn't leave the in-memory entity half-modified.
        if (!AfmValidator.IsValid(newAfm))
            return new UpdateBusinessResult.InvalidAfm();

        // Permission check is per-field: name/kad/address are open to all tenant
        // types, but AFM changes are accountant-only.
        if (newAfm != entity.Afm && !BusinessPermissions.CanEditAfm(_tenant.CurrentTenantType))
            return new UpdateBusinessResult.Forbidden("Δεν επιτρέπεται η αλλαγή ΑΦΜ από αυτόν τον τύπο λογαριασμού.");

        if (newAfm != entity.Afm
            && await _repository.AfmExistsAsync(newAfm, activeOnly: true, excludeId: entity.Id, cancellationToken))
            return new UpdateBusinessResult.ActiveDuplicateAfm(newAfm);

        // Build the diff against the pre-mutation state so the audit captures
        // only fields that actually changed.
        var changes = new Dictionary<string, object?>();
        if (entity.Name != newName)
            changes["Name"] = new { From = entity.Name, To = newName };
        if (entity.Afm != newAfm)
            changes["Afm"] = new { From = entity.Afm, To = newAfm };
        if (entity.Kad != newKad)
            changes["Kad"] = new { From = entity.Kad, To = newKad };
        if (entity.Address != newAddress)
            changes["Address"] = new { From = entity.Address, To = newAddress };

        if (changes.Count == 0)
            return new UpdateBusinessResult.Success();

        // AFM is already validated above; the entity methods will not throw.
        if (entity.Name != newName) entity.Rename(newName);
        if (entity.Afm != newAfm) entity.ChangeAfm(newAfm);
        if (entity.Kad != newKad) entity.UpdateKad(newKad);
        if (entity.Address != newAddress) entity.UpdateAddress(newAddress);

        await _repository.UpdateAsync(entity, cancellationToken);

        await _audit.WriteAsync(
            action: "BusinessUpdated",
            tenantId: _tenant.CurrentTenantId,
            entityType: nameof(Business),
            entityId: entity.Id.ToString(),
            details: changes,
            cancellationToken: cancellationToken);

        return new UpdateBusinessResult.Success();
    }
}
