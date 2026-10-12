namespace Roivo.Application.Features.Accountant.Queries.GetAccountantReport;

/// <summary>
/// Asks for the consolidated monthly report across the accountant's clients.
/// <c>Month</c> is any date inside the month the reconciliation summary
/// covers, and defaults to today — i.e. the current month.
/// </summary>
public sealed record GetAccountantReportQuery(DateOnly? Month = null);

/// <summary>
/// The three consolidated views the reports page renders.
/// </summary>
/// <remarks>
/// <c>From</c> and <c>To</c> bound the reconciliation month.
/// <c>TaxCalendarTo</c> ends the forward window the combined calendar covers:
/// the calendar deliberately looks ahead rather than at the report month, since
/// an accountant reads it to plan payments, not to review a closed period.
/// </remarks>
public sealed record AccountantReport(
    DateOnly From,
    DateOnly To,
    DateOnly TaxCalendarTo,
    IReadOnlyList<AccountantReconciliationRow> ReconciliationSummary,
    AccountantReconciliationTotals ReconciliationTotals,
    IReadOnlyList<AccountantTaxCalendarRow> TaxCalendar,
    IReadOnlyList<AccountantCashflowRow> CashflowOverview);

/// <summary>One client's reconciliation position for the report month.</summary>
public sealed record AccountantReconciliationRow(
    Guid BusinessId,
    string BusinessName,
    string Afm,
    int TotalInvoices,
    int TotalTransactions,
    int ConfirmedMatches,
    int PendingMatches,
    int UnreconciledInvoices,
    int UnreconciledTransactions,
    decimal ReconciledAmount,
    decimal MatchRate);

/// <summary>
/// Column totals under the reconciliation table. <c>AverageMatchRate</c> is
/// the unweighted mean of the per-client rates — zero, not NaN, with no
/// clients.
/// </summary>
public sealed record AccountantReconciliationTotals(
    int Businesses,
    int TotalInvoices,
    int TotalTransactions,
    int ConfirmedMatches,
    int PendingMatches,
    int UnreconciledInvoices,
    decimal ReconciledAmount,
    decimal AverageMatchRate);

/// <summary>One obligation on the combined calendar, with its client named.</summary>
public sealed record AccountantTaxCalendarRow(
    Guid BusinessId,
    string BusinessName,
    string TaxType,
    string Period,
    DateOnly DueDate,
    decimal ExpectedAmount,
    bool IsPaid,
    bool IsOverdue);

/// <summary>
/// One client's projection, for side-by-side comparison. <c>DaysOfRunway</c>
/// and each balance are null when nothing is stored for the client — which is
/// also why <c>Health</c> then reads <c>Unknown</c> rather than a colour.
/// </summary>
public sealed record AccountantCashflowRow(
    Guid BusinessId,
    string BusinessName,
    string Health,
    int? DaysOfRunway,
    decimal? Balance30Day,
    decimal? Balance60Day,
    decimal? Balance90Day,
    decimal PredictedInflow,
    decimal PredictedOutflow);
