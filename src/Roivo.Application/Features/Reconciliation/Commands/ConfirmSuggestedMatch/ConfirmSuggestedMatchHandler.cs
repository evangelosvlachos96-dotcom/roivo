using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Auditing;
using Roivo.Core.Domain.Businesses;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Exceptions;

namespace Roivo.Application.Features.Reconciliation.Commands.ConfirmSuggestedMatch;

public sealed class ConfirmSuggestedMatchHandler
{
    private readonly IReconciliationRepository _repository;
    private readonly IAuditWriter _audit;
    private readonly ITenantContext _tenant;

    public ConfirmSuggestedMatchHandler(
        IReconciliationRepository repository,
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

    public async Task<ConfirmSuggestedMatchResult> Handle(
        ConfirmSuggestedMatchCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!BusinessPermissions.CanReconcileFor(_tenant.CurrentTenantType))
            return new ConfirmSuggestedMatchResult.Forbidden("Δεν επιτρέπεται η αντιστοίχιση από αυτόν τον τύπο λογαριασμού.");

        var match = await _repository.GetMatchByIdAsync(command.MatchId, cancellationToken);
        if (match is null)
            return new ConfirmSuggestedMatchResult.NotFound();

        try
        {
            match.Confirm(command.UserId);
        }
        catch (DomainException)
        {
            // Another reviewer decided this one first; report its state rather
            // than overwriting their decision.
            return new ConfirmSuggestedMatchResult.NotPending(match.Status.ToString());
        }

        await _repository.UpdateMatchAsync(match, cancellationToken);

        await _audit.WriteAsync(
            action: AuditAction.ReconciliationMatchConfirmed,
            tenantId: _tenant.CurrentTenantId,
            entityType: nameof(ReconciliationMatch),
            entityId: match.Id.ToString(),
            details: new Dictionary<string, object?>
            {
                ["InvoiceId"] = match.InvoiceId,
                ["BankTransactionId"] = match.BankTransactionId,
                ["Confidence"] = match.MatchConfidence,
            },
            cancellationToken: cancellationToken);

        return new ConfirmSuggestedMatchResult.Success();
    }
}
