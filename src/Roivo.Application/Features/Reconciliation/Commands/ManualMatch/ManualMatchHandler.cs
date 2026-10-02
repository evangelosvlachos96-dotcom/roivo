using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Auditing;
using Roivo.Core.Domain.Businesses;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Features.Reconciliation.Commands.ManualMatch;

public sealed class ManualMatchHandler
{
    private readonly IReconciliationRepository _repository;
    private readonly IAuditWriter _audit;
    private readonly ITenantContext _tenant;

    public ManualMatchHandler(
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

    public async Task<ManualMatchResult> Handle(
        ManualMatchCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!BusinessPermissions.CanReconcileFor(_tenant.CurrentTenantType))
            return new ManualMatchResult.Forbidden("Δεν επιτρέπεται η αντιστοίχιση από αυτόν τον τύπο λογαριασμού.");

        // Loading through the business confirms both rows belong to the caller.
        // The ids arrive from the client and cannot be trusted on their own.
        var (invoice, transaction) = await _repository.GetPairForBusinessAsync(
            command.BusinessId, command.InvoiceId, command.BankTransactionId, cancellationToken);

        if (invoice is null || transaction is null)
            return new ManualMatchResult.NotFound();

        if (await _repository.PairIsMatchedAsync(command.InvoiceId, command.BankTransactionId, cancellationToken))
            return new ManualMatchResult.AlreadyMatched();

        // Without this the same invoice could be settled twice, and the
        // reconciled totals would overstate the money actually accounted for.
        if (await _repository.EitherSideIsReconciledAsync(command.InvoiceId, command.BankTransactionId, cancellationToken))
            return new ManualMatchResult.SideAlreadyReconciled();

        var match = ReconciliationMatch.CreateManual(
            command.BusinessId,
            command.InvoiceId,
            command.BankTransactionId,
            command.UserId,
            command.Notes);

        await _repository.AddMatchesAsync([match], cancellationToken);

        await _audit.WriteAsync(
            action: AuditAction.ReconciliationMatchCreatedManually,
            tenantId: _tenant.CurrentTenantId,
            entityType: nameof(ReconciliationMatch),
            entityId: match.Id.ToString(),
            details: new Dictionary<string, object?>
            {
                ["BusinessId"] = command.BusinessId,
                ["InvoiceId"] = command.InvoiceId,
                ["BankTransactionId"] = command.BankTransactionId,
            },
            cancellationToken: cancellationToken);

        return new ManualMatchResult.Success(match.Id);
    }
}
