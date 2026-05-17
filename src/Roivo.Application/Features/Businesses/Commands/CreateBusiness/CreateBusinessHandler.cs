using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Validation;

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

        var afm = (command.Afm ?? string.Empty).Trim();
        var name = (command.Name ?? string.Empty).Trim();

        if (!AfmValidator.IsValid(afm))
            return new CreateBusinessResult.InvalidAfm();

        // Check active duplicate first — that's a hard block.
        if (await _repository.AfmExistsAsync(afm, activeOnly: true, excludeId: null, cancellationToken))
            return new CreateBusinessResult.ActiveDuplicate(afm);

        // Check inactive duplicate — surfaces a reactivation option to the caller.
        var inactive = await _repository.FindByAfmAsync(afm, activeOnly: false, cancellationToken);
        if (inactive is not null && !inactive.IsActive)
        {
            return new CreateBusinessResult.InactiveDuplicate(inactive.Id, inactive.Name, inactive.Address);
        }

        var business = new Business
        {
            Name = name,
            Afm = afm,
            Kad = string.IsNullOrWhiteSpace(command.Kad) ? null : command.Kad.Trim(),
            Address = string.IsNullOrWhiteSpace(command.Address) ? null : command.Address.Trim()
            // TenantId is set by ApplicationDbContext.SaveChangesAsync override
        };

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
