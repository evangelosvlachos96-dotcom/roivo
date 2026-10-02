using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Features.Cashflow.Services;

/// <summary>Projects a business's cash position forward from its own history.</summary>
public interface ICashflowForecastEngine
{
    /// <summary>
    /// Builds a day-by-day projection. Pure analysis — nothing is persisted.
    /// </summary>
    Task<CashflowForecastResult> ForecastAsync(
        Guid businessId,
        int daysAhead = 90,
        CancellationToken cancellationToken = default);
}

public sealed record CashflowForecastResult(
    IReadOnlyList<DailyForecast> DailyForecasts,
    CashflowSummary Summary,
    IReadOnlyList<TaxObligation> UpcomingTaxObligations,
    IReadOnlyList<CashflowAlert> Alerts)
{
    public static CashflowForecastResult Empty { get; } = new(
        [],
        new CashflowSummary(0m, 0m, 0m, 0m, 0m, 0m, 0m, 0),
        [],
        []);
}

public sealed record DailyForecast(
    DateOnly Date,
    decimal PredictedInflow,
    decimal PredictedOutflow,
    decimal PredictedBalance,
    decimal ConfidenceLow,
    decimal ConfidenceHigh);

/// <summary>
/// Headline cashflow figures. <c>BurnRate</c> is the average daily net outflow,
/// zero when the business is cash-positive. <c>DaysOfRunway</c> counts days
/// until the projected balance first goes negative, or
/// <see cref="CashflowSummary.UnlimitedRunway"/> when it never does.
/// </summary>
public sealed record CashflowSummary(
    decimal CurrentBalance,
    decimal Forecast30DayBalance,
    decimal Forecast60DayBalance,
    decimal Forecast90DayBalance,
    decimal AverageDailyInflow,
    decimal AverageDailyOutflow,
    decimal BurnRate,
    int DaysOfRunway)
{
    /// <summary>Sentinel for a projection that never runs out of cash.</summary>
    public const int UnlimitedRunway = int.MaxValue;
}

/// <summary>
/// A condition the forecast surfaced. <c>Type</c> is one of
/// <see cref="CashflowAlertType"/>, <c>Severity</c> one of
/// <see cref="CashflowAlertSeverity"/>; <c>Message</c> is English diagnostic
/// text, so the UI renders its own localized string off the type.
/// </summary>
public sealed record CashflowAlert(
    string Type,
    DateOnly Date,
    string Message,
    decimal Amount,
    string Severity);

/// <summary>Alert discriminators. The UI maps these to localized text.</summary>
public static class CashflowAlertType
{
    public const string LowBalance = "LowBalance";
    public const string NegativeBalance = "NegativeBalance";
    public const string TaxDue = "TaxDue";
    public const string LargeExpense = "LargeExpense";
}

public static class CashflowAlertSeverity
{
    public const string Warning = "Warning";
    public const string Critical = "Critical";
}
