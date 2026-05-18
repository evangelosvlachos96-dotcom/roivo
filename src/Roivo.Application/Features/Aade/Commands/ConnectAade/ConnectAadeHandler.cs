using Roivo.Application.Abstractions;
using Roivo.Application.Abstractions.Aade;
using Roivo.Application.Abstractions.Aade.Results;
using Roivo.Core.Domain.Businesses;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Features.Aade.Commands.ConnectAade;

public sealed class ConnectAadeHandler
{
    private readonly IBusinessRepository _businesses;
    private readonly IAadeClient _aade;
    private readonly IAadeCredentialStore _credentials;
    private readonly IAadeRateLimiter _rateLimiter;
    private readonly IAuditWriter _audit;
    private readonly ITenantContext _tenant;

    public ConnectAadeHandler(
        IBusinessRepository businesses,
        IAadeClient aade,
        IAadeCredentialStore credentials,
        IAadeRateLimiter rateLimiter,
        IAuditWriter audit,
        ITenantContext tenant)
    {
        ArgumentNullException.ThrowIfNull(businesses);
        ArgumentNullException.ThrowIfNull(aade);
        ArgumentNullException.ThrowIfNull(credentials);
        ArgumentNullException.ThrowIfNull(rateLimiter);
        ArgumentNullException.ThrowIfNull(audit);
        ArgumentNullException.ThrowIfNull(tenant);

        _businesses = businesses;
        _aade = aade;
        _credentials = credentials;
        _rateLimiter = rateLimiter;
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

        if (!await _rateLimiter.TryAcquireValidationSlotAsync(business.Id, cancellationToken).ConfigureAwait(false))
            return new ConnectAadeResult.RateLimited();

        var validation = await _aade.ValidateCredentialsAsync(command.UserId, command.SubscriptionKey, cancellationToken).ConfigureAwait(false);
        switch (validation)
        {
            case AadeValidationResult.InvalidCredentials:
                return new ConnectAadeResult.InvalidCredentials();
            case AadeValidationResult.NetworkError ne:
                return new ConnectAadeResult.AadeUnavailable(ne.Message);
            case AadeValidationResult.AadeServerError se:
                return new ConnectAadeResult.AadeUnavailable($"AADE returned {se.StatusCode}: {se.Message}");
        }

        var success = (AadeValidationResult.Success)validation;
        if (!string.Equals(success.AfmFromAade, business.Afm, StringComparison.Ordinal))
            return new ConnectAadeResult.AfmMismatch(business.Afm, success.AfmFromAade);

        await _credentials.StoreAsync(business.Id, command.UserId, command.SubscriptionKey, cancellationToken).ConfigureAwait(false);

        // Audit must never include the credentials themselves — only metadata.
        await _audit.WriteAsync(
            action: "AadeConnected",
            tenantId: _tenant.CurrentTenantId,
            entityType: nameof(Business),
            entityId: business.Id.ToString(),
            details: new { business.Name, business.Afm },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new ConnectAadeResult.Success(business.Id);
    }
}
