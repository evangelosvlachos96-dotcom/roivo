namespace Roivo.Application.Features.Reconciliation.Queries.GetReconciliationDashboard;

/// <summary>
/// Dashboard figures for a business over a window. <c>From</c> and <c>To</c>
/// default to the trailing 90 days when omitted.
/// </summary>
public sealed record GetReconciliationDashboardQuery(
    Guid BusinessId,
    DateOnly? From = null,
    DateOnly? To = null,
    int RecentMatchCount = 10);

/// <summary>Everything the reconciliation dashboard renders.</summary>
public sealed record ReconciliationDashboard(
    Guid BusinessId,
    DateOnly From,
    DateOnly To,
    int TotalInvoices,
    int TotalTransactions,
    int ConfirmedMatches,
    int PendingMatches,
    int UnreconciledInvoices,
    int UnreconciledTransactions,
    decimal ReconciledAmount,
    decimal MatchRate,
    IReadOnlyList<ReconciliationMatchListItem> RecentMatches);

/// <summary>A match flattened for display, with both sides resolved.</summary>
public sealed record ReconciliationMatchListItem(
    Guid MatchId,
    Guid InvoiceId,
    Guid BankTransactionId,
    string MatchType,
    string Status,
    decimal Confidence,
    DateTime MatchedAt,
    DateOnly InvoiceIssueDate,
    decimal InvoiceGrossAmount,
    string? InvoiceCounterpartyName,
    DateOnly TransactionBookingDate,
    decimal TransactionAmount,
    string? TransactionCounterpartyName);
