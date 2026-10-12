using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Features.Cashflow.Services;

/// <summary>
/// Projects cash forward by blending a day-of-week profile with a day-of-month
/// profile drawn from the last year of bank activity, then layering on the
/// commitments the business has told us about.
/// </summary>
/// <remarks>
/// Two seasonal profiles rather than one: Greek SMB receipts cluster by weekday
/// (retail weekends, B2B Fridays) while costs cluster by month day (rent on the
/// 1st, payroll at month end). Averaging both captures each without needing a
/// model the data could not support.
/// </remarks>
public sealed class CashflowForecastEngine : ICashflowForecastEngine
{
    /// <summary>History window the profiles are built from.</summary>
    public const int LookbackDays = 365;

    /// <summary>Minimum days of history before a forecast is worth producing.</summary>
    public const int MinimumHistoryDays = 30;

    /// <summary>Balance at or below which a warning is raised.</summary>
    public const decimal LowBalanceThreshold = 1000m;

    /// <summary>Daily outflow above this multiple of the average is called out.</summary>
    private const decimal LargeExpenseMultiple = 3m;

    /// <summary>
    /// Band half-width as a fraction of the cumulative projected movement, at
    /// one day out. Uncertainty grows with the square root of the horizon, the
    /// standard random-walk assumption.
    /// </summary>
    private const decimal BaseUncertainty = 0.15m;

    private readonly ICashflowRepository _repository;
    private readonly IGreekTaxCalendar _taxCalendar;

    public CashflowForecastEngine(ICashflowRepository repository, IGreekTaxCalendar taxCalendar)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(taxCalendar);

        _repository = repository;
        _taxCalendar = taxCalendar;
    }

    public async Task<CashflowForecastResult> ForecastAsync(
        Guid businessId,
        int daysAhead = 90,
        CancellationToken cancellationToken = default)
    {
        if (daysAhead <= 0)
            return CashflowForecastResult.Empty;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var historyFrom = today.AddDays(-LookbackDays);

        var movements = await _repository
            .ListDailyMovementsAsync(businessId, historyFrom, today, cancellationToken)
            .ConfigureAwait(false);

        var currentBalance = await _repository
            .GetCurrentBalanceAsync(businessId, cancellationToken)
            .ConfigureAwait(false);

        if (movements.Count == 0)
        {
            // No history means no basis for a projection. A flat line at the
            // current balance would read as a prediction; an empty result says
            // honestly that there is nothing to go on.
            return CashflowForecastResult.Empty with
            {
                Summary = new CashflowSummary(
                    currentBalance, currentBalance, currentBalance, currentBalance,
                    0m, 0m, 0m, CashflowSummary.UnlimitedRunway),
            };
        }

        var categories = await _repository.ListCategoriesAsync(businessId, cancellationToken).ConfigureAwait(false);
        var horizonEnd = today.AddDays(daysAhead);

        var obligations = await _repository
            .ListTaxObligationsAsync(businessId, today, horizonEnd, cancellationToken)
            .ConfigureAwait(false);

        var profile = BuildProfile(movements);

        var dailyForecasts = new List<DailyForecast>(daysAhead);
        var alerts = new List<CashflowAlert>();

        var runningBalance = currentBalance;
        var cumulativeMovement = 0m;
        var runwayDays = CashflowSummary.UnlimitedRunway;
        var negativeAlertRaised = false;
        var lowBalanceAlertRaised = false;

        for (var offset = 1; offset <= daysAhead; offset++)
        {
            var date = today.AddDays(offset);

            var inflow = profile.InflowFor(date);
            var outflow = profile.OutflowFor(date);

            foreach (var category in categories)
            {
                if (category.OccurrenceIn(date.Year, date.Month) != date)
                    continue;

                if (category.Type == CashflowCategoryType.Income)
                    inflow += category.AverageAmount ?? 0m;
                else
                    outflow += category.AverageAmount ?? 0m;
            }

            var taxDueToday = obligations
                .Where(o => !o.IsPaid && o.DueDate == date)
                .Sum(o => o.ExpectedAmount);

            outflow += taxDueToday;

            runningBalance += inflow - outflow;
            cumulativeMovement += inflow + outflow;

            // Widen with the square root of the horizon: errors accumulate as a
            // random walk, not linearly.
            var spread = cumulativeMovement * BaseUncertainty * (decimal)Math.Sqrt(offset);

            dailyForecasts.Add(new DailyForecast(
                Date: date,
                PredictedInflow: Round(inflow),
                PredictedOutflow: Round(outflow),
                PredictedBalance: Round(runningBalance),
                ConfidenceLow: Round(runningBalance - spread),
                ConfidenceHigh: Round(runningBalance + spread)));

            if (runningBalance < 0m && !negativeAlertRaised)
            {
                negativeAlertRaised = true;
                runwayDays = offset - 1;
                alerts.Add(new CashflowAlert(
                    CashflowAlertType.NegativeBalance, date,
                    "Projected balance goes negative.", Round(runningBalance),
                    CashflowAlertSeverity.Critical));
            }
            else if (runningBalance < LowBalanceThreshold && !lowBalanceAlertRaised && !negativeAlertRaised)
            {
                lowBalanceAlertRaised = true;
                alerts.Add(new CashflowAlert(
                    CashflowAlertType.LowBalance, date,
                    "Projected balance falls below the low-balance threshold.", Round(runningBalance),
                    CashflowAlertSeverity.Warning));
            }

            if (taxDueToday > 0m)
            {
                alerts.Add(new CashflowAlert(
                    CashflowAlertType.TaxDue, date,
                    "Tax obligation falls due.", Round(taxDueToday),
                    runningBalance < taxDueToday ? CashflowAlertSeverity.Critical : CashflowAlertSeverity.Warning));
            }

            if (profile.AverageOutflow > 0m && outflow > profile.AverageOutflow * LargeExpenseMultiple)
            {
                alerts.Add(new CashflowAlert(
                    CashflowAlertType.LargeExpense, date,
                    "Outflow well above the daily average.", Round(outflow),
                    CashflowAlertSeverity.Warning));
            }
        }

        var summary = new CashflowSummary(
            CurrentBalance: Round(currentBalance),
            Forecast30DayBalance: BalanceAt(dailyForecasts, 30, currentBalance),
            Forecast60DayBalance: BalanceAt(dailyForecasts, 60, currentBalance),
            Forecast90DayBalance: BalanceAt(dailyForecasts, 90, currentBalance),
            AverageDailyInflow: Round(profile.AverageInflow),
            AverageDailyOutflow: Round(profile.AverageOutflow),
            BurnRate: Round(Math.Max(0m, profile.AverageOutflow - profile.AverageInflow)),
            DaysOfRunway: runwayDays);

        return new CashflowForecastResult(
            dailyForecasts,
            summary,
            [.. obligations.Where(o => !o.IsPaid).OrderBy(o => o.DueDate)],
            alerts);
    }

    private static decimal BalanceAt(IReadOnlyList<DailyForecast> forecasts, int day, decimal fallback)
    {
        if (forecasts.Count == 0)
            return Round(fallback);

        // Short horizons report their own last day rather than nothing.
        var index = Math.Min(day, forecasts.Count) - 1;
        return forecasts[index].PredictedBalance;
    }

    internal static CashProfile BuildProfile(IReadOnlyList<DailyCashMovement> movements)
    {
        var byWeekday = movements
            .GroupBy(m => m.Date.DayOfWeek)
            .ToDictionary(
                g => g.Key,
                g => (Inflow: g.Average(m => m.Inflow), Outflow: g.Average(m => m.Outflow)));

        var byMonthDay = movements
            .GroupBy(m => m.Date.Day)
            .ToDictionary(
                g => g.Key,
                g => (Inflow: g.Average(m => m.Inflow), Outflow: g.Average(m => m.Outflow)));

        var averageInflow = movements.Average(m => m.Inflow);
        var averageOutflow = movements.Average(m => m.Outflow);

        return new CashProfile(byWeekday, byMonthDay, averageInflow, averageOutflow);
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    /// <summary>Weekday and month-day averages distilled from history.</summary>
    internal sealed record CashProfile(
        IReadOnlyDictionary<DayOfWeek, (decimal Inflow, decimal Outflow)> ByWeekday,
        IReadOnlyDictionary<int, (decimal Inflow, decimal Outflow)> ByMonthDay,
        decimal AverageInflow,
        decimal AverageOutflow)
    {
        public decimal InflowFor(DateOnly date) => Blend(
            ByWeekday.TryGetValue(date.DayOfWeek, out var w) ? w.Inflow : AverageInflow,
            ByMonthDay.TryGetValue(date.Day, out var m) ? m.Inflow : AverageInflow);

        public decimal OutflowFor(DateOnly date) => Blend(
            ByWeekday.TryGetValue(date.DayOfWeek, out var w) ? w.Outflow : AverageOutflow,
            ByMonthDay.TryGetValue(date.Day, out var m) ? m.Outflow : AverageOutflow);

        // Equal weight: neither profile is reliably the better predictor across
        // the mix of businesses Roivo serves.
        private static decimal Blend(decimal weekday, decimal monthDay) => (weekday + monthDay) / 2m;
    }
}
