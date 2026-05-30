using Roivo.Application.Abstractions;
using Roivo.Application.Abstractions.Aade;
using Roivo.Application.Abstractions.Aade.Results;
using Roivo.Core.Domain.Auditing;
using Roivo.Core.Domain.Businesses;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Features.Aade.Commands.ConnectAade;

public sealed class ConnectAadeHandler
{
    private readonly IBusinessRepository _businesses;
    private readonly IAadeClient _aade;
    private readonly IAadeCredentialStore _credentials;
    private readonly IAuditWriter _audit;
    private readonly ITenantContext _tenant;

    public ConnectAadeHandler(
        IBusinessRepository businesses,
        IAadeClient aade,
        IAadeCredentialStore credentials,
        IAuditWriter audit,
        ITenantContext tenant)
    {
        ArgumentNullException.ThrowIfNull(businesses);
        ArgumentNullException.ThrowIfNull(aade);
        ArgumentNullException.ThrowIfNull(credentials);
        ArgumentNullException.ThrowIfNull(audit);
        ArgumentNullException.ThrowIfNull(tenant);

        _businesses = businesses;
        _aade = aade;
        _credentials = credentials;
        _audit = audit;
        _tenant = tenant;
    }

    public async Task<ConnectAadeResult> Handle(ConnectAadeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!BusinessPermissions.CanManageAadeFor(_tenant.CurrentTenantType))
            return new ConnectAadeResult.Forbidden("Δεν επιτρέπεται η διαχείριση σύνδεσης AADE από αυτόν τον τύπο λογαριασμού.");

        var business = await _businesses.GetByIdActiveOnlyAsync(command.BusinessId, cancellationToken).ConfigureAwait(false);
        if (business is null)
            return new ConnectAadeResult.BusinessNotFound();

        // AADE's RequestDocs endpoint validates credentials AND authorization
        // for the supplied AFM in one shot, so no local rate limiter is needed
        // — AADE itself returns 429 when we hammer.
        var validation = await _aade.ValidateCredentialsAsync(command.UserId, command.SubscriptionKey, business.Afm, cancellationToken).ConfigureAwait(false);

        switch (validation)
        {
            case AadeValidationResult.InvalidCredentials:
                return new ConnectAadeResult.InvalidCredentials();
            case AadeValidationResult.AfmMismatch v:
                // Forensics: track which AFMs an account tries to authorize against.
                await _audit.WriteAsync(
                    action: AuditAction.AadeConnectionAfmMismatch,
                    tenantId: _tenant.CurrentTenantId,
                    entityType: nameof(Business),
                    entityId: business.Id.ToString(),
                    details: new { business.Afm, CredentialsAfm = v.CredentialsAfm },
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                return new ConnectAadeResult.AfmMismatch(business.Afm, v.CredentialsAfm);
            case AadeValidationResult.RateLimited:
                return new ConnectAadeResult.RateLimited();
            case AadeValidationResult.NetworkError ne:
                return new ConnectAadeResult.AadeUnavailable(ne.Message);
            case AadeValidationResult.AadeServerError se:
                return new ConnectAadeResult.AadeUnavailable($"AADE returned {se.StatusCode}: {se.Message}");
        }

        await _credentials.StoreAsync(business.Id, command.UserId, command.SubscriptionKey, cancellationToken).ConfigureAwait(false);

        // A successful reconnect heals any prior failure-tracking state.
        business.ClearAadeSyncFailure();
        await _businesses.UpdateAsync(business, cancellationToken).ConfigureAwait(false);

        // Audit must never include the credentials themselves — only metadata.
        await _audit.WriteAsync(
            action: AuditAction.AadeConnected,
            tenantId: _tenant.CurrentTenantId,
            entityType: nameof(Business),
            entityId: business.Id.ToString(),
            details: new { business.Name, business.Afm },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new ConnectAadeResult.Success(business.Id);
    }
}
