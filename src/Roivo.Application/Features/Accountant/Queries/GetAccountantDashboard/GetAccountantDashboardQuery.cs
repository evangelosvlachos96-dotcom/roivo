namespace Roivo.Application.Features.Accountant.Queries.GetAccountantDashboard;

/// <summary>
/// Asks for one row per business the signed-in accountant manages.
/// </summary>
/// <remarks>
/// <c>ReconciliationWindowDays</c> is the trailing window the per-business
/// match rate is measured over. It defaults to the same ninety days the
/// single-business reconciliation dashboard uses, so the two screens never
/// disagree about a client's percentage.
/// </remarks>
public sealed record GetAccountantDashboardQuery(int ReconciliationWindowDays = 90);

/// <summary>Everything the accountant dashboard renders.</summary>
public sealed record AccountantDashboard(
    AccountantDashboardSummary Summary,
    IReadOnlyList<AccountantBusinessRow> Businesses);

/// <summary>
/// The headline figures above the client list. <c>AverageMatchRate</c> is the
/// unweighted mean of the per-business rates as a 0–1 fraction, and is zero —
/// never NaN — when the accountant manages no businesses yet.
/// </summary>
public sealed record AccountantDashboardSummary(
    int TotalBusinesses,
    decimal AverageMatchRate,
    int BusinessesWithCashflowWarnings,
    int TaxDeadlinesNext30Days);

/// <summary>
/// One client's health at a glance.
/// </summary>
/// <remarks>
/// <c>CashflowHealth</c> is one of the
/// <see cref="Roivo.Application.Features.Accountant.Services.CashflowHealth"/>
/// colours; <c>Unknown</c> means no stored projection, which is deliberately
/// not the same as healthy. <c>DaysOfRunway</c> counts the days before that
/// projection first goes negative — null when there is none, and
/// <see cref="Roivo.Application.Features.Cashflow.Services.CashflowSummary.UnlimitedRunway"/>
/// when it never does.
/// </remarks>
public sealed record AccountantBusinessRow(
    Guid BusinessId,
    string Name,
    string Afm,
    bool AadeConnected,
    bool BankingConnected,
    int TotalInvoices,
    int ConfirmedMatches,
    int UnreconciledInvoices,
    decimal MatchRate,
    string CashflowHealth,
    int? DaysOfRunway,
    int TaxDeadlinesNext30Days,
    DateTime? LastAadeSyncAt,
    DateTime? LastBankingSyncAt);
