using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Auditing;
using Roivo.Core.Domain.Businesses;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Exceptions;

namespace Roivo.Application.Features.Reconciliation.Commands.RejectSuggestedMatch;

public sealed class RejectSuggestedMatchHandler
{
    private readonly IReconciliationRepository _repository;
    private readonly IAuditWriter _audit;
    private readonly ITenantContext _tenant;

    public RejectSuggestedMatchHandler(
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

    public async Task<RejectSuggestedMatchResult> Handle(
        RejectSuggestedMatchCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!BusinessPermissions.CanReconcileFor(_tenant.CurrentTenantType))
            return new RejectSuggestedMatchResult.Forbidden("Δεν επιτρέπεται η αντιστοίχιση από αυτόν τον τύπο λογαριασμού.");

        var match = await _repository.GetMatchByIdAsync(command.MatchId, cancellationToken);
        if (match is null)
            return new RejectSuggestedMatchResult.NotFound();

        try
        {
            match.Reject(command.UserId, command.Notes);
        }
        catch (DomainException)
        {
            return new RejectSuggestedMatchResult.NotPending(match.Status.ToString());
        }

        await _repository.UpdateMatchAsync(match, cancellationToken);

        await _audit.WriteAsync(
            action: AuditAction.ReconciliationMatchRejected,
            tenantId: _tenant.CurrentTenantId,
            entityType: nameof(ReconciliationMatch),
            entityId: match.Id.ToString(),
            details: new Dictionary<string, object?>
            {
                ["InvoiceId"] = match.InvoiceId,
                ["BankTransactionId"] = match.BankTransactionId,
            },
            cancellationToken: cancellationToken);

        return new RejectSuggestedMatchResult.Success();
    }
}
