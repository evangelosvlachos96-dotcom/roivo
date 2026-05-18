using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Businesses;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Exceptions;

namespace Roivo.Application.Features.Businesses.Commands.CreateBusiness;

public sealed class CreateBusinessHandler
{
    private readonly IBusinessRepository _repository;
    private readonly IAuditWriter _audit;
    private readonly ITenantContext _tenant;

    public CreateBusinessHandler(
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

    public async Task<CreateBusinessResult> Handle(CreateBusinessCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!BusinessPermissions.CanCreate(_tenant.CurrentTenantType))
            return new CreateBusinessResult.Forbidden("Δεν επιτρέπεται η δημιουργία νέας επιχείρησης από αυτόν τον τύπο λογαριασμού.");

        Business business;
        try
        {
            business = Business.Create(command.Name, command.Afm, command.Kad, command.Address);
        }
        catch (InvalidAfmException)
        {
            return new CreateBusinessResult.InvalidAfm();
        }

        // Run duplicate checks against the normalized AFM before persisting so
        // callers see the dedicated result variants instead of a DB exception.
        if (await _repository.AfmExistsAsync(business.Afm, activeOnly: true, excludeId: null, cancellationToken))
            return new CreateBusinessResult.ActiveDuplicate(business.Afm);

        var inactive = await _repository.FindByAfmAsync(business.Afm, activeOnly: false, cancellationToken);
        if (inactive is not null && !inactive.IsActive)
        {
            return new CreateBusinessResult.InactiveDuplicate(inactive.Id, inactive.Name, inactive.Address);
        }

        business = await _repository.AddAsync(business, cancellationToken);

        await _audit.WriteAsync(
            action: "BusinessCreated",
            tenantId: _tenant.CurrentTenantId,
            entityType: nameof(Business),
            entityId: business.Id.ToString(),
            details: new { business.Name, business.Afm },
            cancellationToken: cancellationToken);

        return new CreateBusinessResult.Success(business.Id);
    }
}
