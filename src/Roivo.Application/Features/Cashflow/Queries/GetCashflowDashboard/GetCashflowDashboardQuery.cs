using Roivo.Application.Features.Cashflow.Services;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Features.Cashflow.Queries.GetCashflowDashboard;

public sealed record GetCashflowDashboardQuery(Guid BusinessId, int DaysAhead = 90);

/// <summary>
/// Everything the cashflow dashboard renders. <c>HasSufficientHistory</c> is
/// false when the business has fewer than
/// <see cref="CashflowForecastEngine.MinimumHistoryDays"/> days of transactions,
/// and the UI then shows an explanation rather than a chart built on nothing.
/// </summary>
public sealed record CashflowDashboard(
    Guid BusinessId,
    bool HasSufficientHistory,
    CashflowSummary Summary,
    IReadOnlyList<DailyForecast> DailyForecasts,
    IReadOnlyList<TaxObligation> UpcomingTaxObligations,
    IReadOnlyList<CashflowAlert> Alerts);
