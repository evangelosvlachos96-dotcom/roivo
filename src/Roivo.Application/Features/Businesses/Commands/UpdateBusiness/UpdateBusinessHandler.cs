using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;
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

        var afm = (command.Afm ?? string.Empty).Trim();
        var name = (command.Name ?? string.Empty).Trim();
        var kad = string.IsNullOrWhiteSpace(command.Kad) ? null : command.Kad.Trim();
        var address = string.IsNullOrWhiteSpace(command.Address) ? null : command.Address.Trim();

        if (!AfmValidator.IsValid(afm))
            return new UpdateBusinessResult.InvalidAfm();

        var entity = await _repository.GetByIdActiveOnlyAsync(command.Id, cancellationToken);
        if (entity is null)
            return new UpdateBusinessResult.NotFound();

        // Exclude self so saving an unchanged AFM doesn't collide.
        if (await _repository.AfmExistsAsync(afm, activeOnly: true, excludeId: entity.Id, cancellationToken))
            return new UpdateBusinessResult.ActiveDuplicateAfm(afm);

        var changes = new Dictionary<string, object?>();
        if (entity.Name != name)
            changes["Name"] = new { From = entity.Name, To = name };
        if (entity.Afm != afm)
            changes["Afm"] = new { From = entity.Afm, To = afm };
        if (entity.Kad != kad)
            changes["Kad"] = new { From = entity.Kad, To = kad };
        if (entity.Address != address)
            changes["Address"] = new { From = entity.Address, To = address };

        if (changes.Count == 0)
            return new UpdateBusinessResult.Success();

        entity.Name = name;
        entity.Afm = afm;
        entity.Kad = kad;
        entity.Address = address;

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
