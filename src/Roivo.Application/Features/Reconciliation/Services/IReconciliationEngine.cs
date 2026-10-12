using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Features.Reconciliation.Services;

/// <summary>
/// Pairs invoices with the bank transactions that paid them.
/// </summary>
public interface IReconciliationEngine
{
    /// <summary>
    /// Scores every unreconciled invoice in the window against every
    /// unreconciled transaction and partitions the result. Pure analysis —
    /// nothing is persisted; <see cref="Commands.RunReconciliation.RunReconciliationHandler"/>
    /// decides what to save.
    /// </summary>
    Task<ReconciliationResult> ReconcileAsync(
        Guid businessId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);
}

/// <summary>Outcome of a reconciliation pass.</summary>
public sealed record ReconciliationResult(
    IReadOnlyList<ReconciliationMatch> AutoMatched,
    IReadOnlyList<ReconciliationMatch> Suggested,
    IReadOnlyList<Invoice> UnmatchedInvoices,
    IReadOnlyList<BankTransaction> UnmatchedTransactions,
    ReconciliationSummary Summary)
{
    public static ReconciliationResult Empty { get; } = new(
        [], [], [], [],
        new ReconciliationSummary(0, 0, 0, 0, 0, 0, 0m, 0m));
}

/// <summary>
/// Headline numbers for a reconciliation pass. <c>AutoMatchRate</c> is automatic
/// matches as a fraction of the invoices considered, in [0,1], and zero when
/// there were no invoices.
/// </summary>
public sealed record ReconciliationSummary(
    int TotalInvoices,
    int TotalTransactions,
    int AutoMatchedCount,
    int SuggestedCount,
    int UnmatchedInvoiceCount,
    int UnmatchedTransactionCount,
    decimal AutoMatchRate,
    decimal TotalReconciledAmount);
