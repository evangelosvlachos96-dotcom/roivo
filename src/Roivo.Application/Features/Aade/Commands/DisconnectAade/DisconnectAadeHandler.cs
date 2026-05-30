using Roivo.Application.Abstractions;
using Roivo.Application.Abstractions.Aade;
using Roivo.Core.Domain.Auditing;
using Roivo.Core.Domain.Businesses;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Features.Aade.Commands.DisconnectAade;

public sealed class DisconnectAadeHandler
{
    private readonly IBusinessRepository _businesses;
    private readonly IAadeCredentialStore _credentials;
    private readonly IAuditWriter _audit;
    private readonly ITenantContext _tenant;

    public DisconnectAadeHandler(
        IBusinessRepository businesses,
        IAadeCredentialStore credentials,
        IAuditWriter audit,
        ITenantContext tenant)
    {
        ArgumentNullException.ThrowIfNull(businesses);
        ArgumentNullException.ThrowIfNull(credentials);
        ArgumentNullException.ThrowIfNull(audit);
        ArgumentNullException.ThrowIfNull(tenant);

        _businesses = businesses;
        _credentials = credentials;
        _audit = audit;
        _tenant = tenant;
    }

    public async Task<DisconnectAadeResult> Handle(DisconnectAadeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!BusinessPermissions.CanManageAadeFor(_tenant.CurrentTenantType))
            return new DisconnectAadeResult.Forbidden("Δεν επιτρέπεται η διαχείριση σύνδεσης AADE από αυτόν τον τύπο λογαριασμού.");

        var business = await _businesses.GetByIdActiveOnlyAsync(command.BusinessId, cancellationToken).ConfigureAwait(false);
        if (business is null)
            return new DisconnectAadeResult.BusinessNotFound();

        await _credentials.ClearAsync(business.Id, cancellationToken).ConfigureAwait(false);

        // No point tracking failure on a disconnected business.
        business.ClearAadeSyncFailure();
        await _businesses.UpdateAsync(business, cancellationToken).ConfigureAwait(false);

        await _audit.WriteAsync(
            action: AuditAction.AadeDisconnected,
            tenantId: _tenant.CurrentTenantId,
            entityType: nameof(Business),
            entityId: business.Id.ToString(),
            details: new { business.Name, business.Afm },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new DisconnectAadeResult.Success();
    }
}
