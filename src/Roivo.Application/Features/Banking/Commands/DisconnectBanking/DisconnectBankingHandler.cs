using Microsoft.Extensions.Logging;
using Roivo.Application.Abstractions;
using Roivo.Application.Abstractions.Banking;
using Roivo.Application.Abstractions.Banking.Results;
using Roivo.Core.Domain.Auditing;
using Roivo.Core.Domain.Businesses;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Features.Banking.Commands.DisconnectBanking;

public sealed class DisconnectBankingHandler
{
    private readonly IBusinessRepository _businesses;
    private readonly IBankAccountRepository _accounts;
    private readonly IBankingClient _banking;
    private readonly IBankingCredentialStore _credentials;
    private readonly IAuditWriter _audit;
    private readonly ITenantContext _tenant;
    private readonly ILogger<DisconnectBankingHandler> _logger;

    public DisconnectBankingHandler(
        IBusinessRepository businesses,
        IBankAccountRepository accounts,
        IBankingClient banking,
        IBankingCredentialStore credentials,
        IAuditWriter audit,
        ITenantContext tenant,
        ILogger<DisconnectBankingHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(businesses);
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(banking);
        ArgumentNullException.ThrowIfNull(credentials);
        ArgumentNullException.ThrowIfNull(audit);
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentNullException.ThrowIfNull(logger);

        _businesses = businesses;
        _accounts = accounts;
        _banking = banking;
        _credentials = credentials;
        _audit = audit;
        _tenant = tenant;
        _logger = logger;
    }

    public async Task<DisconnectBankingResult> Handle(DisconnectBankingCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!BusinessPermissions.CanManageBankingFor(_tenant.CurrentTenantType))
            return new DisconnectBankingResult.Forbidden("Δεν επιτρέπεται η διαχείριση τραπεζικής σύνδεσης από αυτόν τον τύπο λογαριασμού.");

        var business = await _businesses.GetByIdActiveOnlyAsync(command.BusinessId, cancellationToken).ConfigureAwait(false);
        if (business is null)
            return new DisconnectBankingResult.BusinessNotFound();

        // Release the bank-side consent first, but never let that failure block
        // the local disconnect — the user asked to be disconnected, and a
        // stranded consent expires on its own within 90 days.
        var sessionId = await _credentials.RetrieveAsync(business.Id, cancellationToken).ConfigureAwait(false);
        if (sessionId is not null)
        {
            var revoke = await _banking.RevokeSessionAsync(sessionId, cancellationToken).ConfigureAwait(false);
            if (revoke is BankingRevokeResult.Failed failed)
            {
                _logger.LogWarning(
                    "Could not revoke the banking session for business {BusinessId} at the aggregator; clearing it locally anyway: {Reason}",
                    business.Id, failed.Message);
            }
        }

        await _credentials.ClearAsync(business.Id, cancellationToken).ConfigureAwait(false);

        // Accounts (and their transactions) go with the connection: keeping them
        // would show stale balances that silently stop updating.
        await _accounts.DeleteByBusinessAsync(business.Id, cancellationToken).ConfigureAwait(false);

        business.ClearBankingConnection();
        await _businesses.UpdateAsync(business, cancellationToken).ConfigureAwait(false);

        await _audit.WriteAsync(
            action: AuditAction.BankingConnectionRevoked,
            tenantId: _tenant.CurrentTenantId,
            entityType: nameof(Business),
            entityId: business.Id.ToString(),
            details: new { business.Name, business.Afm },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new DisconnectBankingResult.Success();
    }
}
