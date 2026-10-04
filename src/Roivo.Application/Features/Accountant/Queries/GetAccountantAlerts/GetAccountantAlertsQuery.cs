namespace Roivo.Application.Features.Accountant.Queries.GetAccountantAlerts;

/// <summary>
/// Asks for every condition across the accountant's client book that wants
/// attention. <c>ReconciliationWindowDays</c> is the trailing window the match
/// rate behind the low-reconciliation alert is measured over.
/// </summary>
public sealed record GetAccountantAlertsQuery(int ReconciliationWindowDays = 90);

/// <summary>
/// One thing an accountant should look at, on one named client.
/// </summary>
/// <remarks>
/// <c>Type</c> is one of
/// <see cref="Roivo.Application.Features.Accountant.Services.AccountantAlertType"/>
/// and <c>Severity</c> one of
/// <see cref="Roivo.Application.Features.Accountant.Services.AccountantAlertSeverity"/>;
/// the UI turns both into localized text, so no Greek crosses this boundary.
/// <c>Date</c> is the date the alert is about — a due date, or the day cash
/// runs out. <c>Amount</c> and <c>Rate</c> carry the figure when the alert has
/// one. <c>Reason</c> is the integration's own short, unlocalized
/// failure-category tag ("InvalidCredentials", "SessionExpired").
/// </remarks>
public sealed record AccountantAlert(
    Guid BusinessId,
    string BusinessName,
    string Type,
    string Severity,
    DateOnly? Date = null,
    decimal? Amount = null,
    decimal? Rate = null,
    string? Reason = null);
