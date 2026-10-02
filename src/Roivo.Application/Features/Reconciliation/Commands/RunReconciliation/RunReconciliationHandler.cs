using Roivo.Application.Abstractions;
using Roivo.Application.Features.Reconciliation.Services;
using Roivo.Core.Domain.Auditing;
using Roivo.Core.Domain.Businesses;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Features.Reconciliation.Commands.RunReconciliation;

public sealed class RunReconciliationHandler
{
    private readonly IReconciliationEngine _engine;
    private readonly IReconciliationRepository _repository;
    private readonly IBusinessRepository _businesses;
    private readonly IAuditWriter _audit;
    private readonly ITenantContext _tenant;

    public RunReconciliationHandler(
        IReconciliationEngine engine,
        IReconciliationRepository repository,
        IBusinessRepository businesses,
        IAuditWriter audit,
        ITenantContext tenant)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(businesses);
        ArgumentNullException.ThrowIfNull(audit);
        ArgumentNullException.ThrowIfNull(tenant);

        _engine = engine;
        _repository = repository;
        _businesses = businesses;
        _audit = audit;
        _tenant = tenant;
    }

    public async Task<RunReconciliationResult> Handle(
        RunReconciliationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.To < command.From)
            return new RunReconciliationResult.InvalidDateRange();

        if (!command.BypassTenantScope && !BusinessPermissions.CanReconcileFor(_tenant.CurrentTenantType))
            return new RunReconciliationResult.Forbidden("Δεν επιτρέπεται η αντιστοίχιση από αυτόν τον τύπο λογαριασμού.");

        // The nightly cron has no ambient tenant, so the filtered lookup would
        // return null for every business. Same trap the banking sync hit.
        var business = command.BypassTenantScope
            ? await _businesses.GetByIdActiveOnlyAcrossAllTenantsAsync(command.BusinessId, cancellationToken)
            : await _businesses.GetByIdActiveOnlyAsync(command.BusinessId, cancellationToken);

        if (business is null)
            return new RunReconciliationResult.NotFound();

        var result = await _engine
            .ReconcileAsync(command.BusinessId, command.From, command.To, cancellationToken)
            .ConfigureAwait(false);

        // Only automatic matches are persisted as confirmed. Suggestions are
        // written too, as Pending rows, so the review queue survives a restart
        // and a rejection can be remembered.
        var toPersist = new List<ReconciliationMatch>(result.AutoMatched.Count + result.Suggested.Count);
        toPersist.AddRange(result.AutoMatched);
        toPersist.AddRange(result.Suggested);

        var persisted = toPersist.Count == 0
            ? 0
            : await _repository.AddMatchesAsync(toPersist, cancellationToken).ConfigureAwait(false);

        await _audit.WriteAsync(
            action: AuditAction.ReconciliationRun,
            tenantId: command.BypassTenantScope ? business.TenantId : _tenant.CurrentTenantId,
            entityType: nameof(Business),
            entityId: business.Id.ToString(),
            details: new Dictionary<string, object?>
            {
                ["From"] = command.From.ToString("yyyy-MM-dd"),
                ["To"] = command.To.ToString("yyyy-MM-dd"),
                ["AutoMatched"] = result.Summary.AutoMatchedCount,
                ["Suggested"] = result.Summary.SuggestedCount,
                ["UnmatchedInvoices"] = result.Summary.UnmatchedInvoiceCount,
                ["UnmatchedTransactions"] = result.Summary.UnmatchedTransactionCount,
                ["Persisted"] = persisted,
            },
            cancellationToken: cancellationToken);

        return new RunReconciliationResult.Success(result, persisted);
    }
}
