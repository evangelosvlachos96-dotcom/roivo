using Roivo.Application.Features.Cashflow.Services;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Features.Accountant.Services;

/// <summary>
/// Traffic-light reading of a business's cash position, derived from days of
/// runway. Pure functions so both the dashboard and the report classify
/// identically, and so the thresholds are testable at their boundaries.
/// </summary>
public static class CashflowHealth
{
    /// <summary>Stays solvent across the whole forecast horizon.</summary>
    public const string Green = "Green";

    /// <summary>Solvent today, but the projection runs dry inside the horizon.</summary>
    public const string Yellow = "Yellow";

    /// <summary>Runs dry inside one filing/payroll cycle.</summary>
    public const string Red = "Red";

    /// <summary>No stored projection to judge — not the same as healthy.</summary>
    public const string Unknown = "Unknown";

    /// <summary>
    /// Below this many days of runway the position is <see cref="Red"/>. Thirty
    /// days is one ΦΠΑ/ΕΦΚΑ filing cycle: inside it the accountant can no
    /// longer fix the shortfall by rescheduling a payment, only by finding
    /// money.
    /// </summary>
    public const int RedBelowDays = 30;

    /// <summary>
    /// At or above this many days of runway the position is <see cref="Green"/>.
    /// Ninety days is the forecast horizon itself, so Green means "the
    /// projection never predicts a shortfall", which is the only claim the
    /// engine can actually support.
    /// </summary>
    public const int GreenAtOrAboveDays = 90;

    /// <summary>
    /// Classifies a runway in days. Null — no stored forecast — maps to
    /// <see cref="Unknown"/> rather than to a colour, so a business the nightly
    /// job has never reached is not reported as healthy.
    /// <see cref="CashflowSummary.UnlimitedRunway"/> is Green by construction.
    /// </summary>
    public static string FromDaysOfRunway(int? daysOfRunway) => daysOfRunway switch
    {
        null => Unknown,
        < RedBelowDays => Red,
        < GreenAtOrAboveDays => Yellow,
        _ => Green,
    };

    /// <summary>True for the colours that should count as a cashflow warning.</summary>
    public static bool IsWarning(string health) => health is Yellow or Red;

    /// <summary>
    /// Days of runway implied by a business's stored projection, or null when
    /// nothing is stored for it.
    /// </summary>
    /// <remarks>
    /// Mirrors <c>CashflowForecastEngine</c> exactly — the count is the number
    /// of whole days before the balance first goes negative, so a projection
    /// that is already negative tomorrow has zero runway. Reading the stored
    /// rows rather than re-running the engine is what keeps a 50-business
    /// dashboard to a single query instead of fifty forecast runs.
    /// </remarks>
    public static int? RunwayFromStoredForecasts(
        IEnumerable<CashflowForecast> forecasts,
        DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(forecasts);

        var any = false;
        var firstNegative = (DateOnly?)null;

        foreach (var forecast in forecasts)
        {
            if (forecast.ForecastDate <= today)
                continue;

            any = true;

            if (forecast.PredictedBalance < 0m
                && (firstNegative is null || forecast.ForecastDate < firstNegative))
            {
                firstNegative = forecast.ForecastDate;
            }
        }

        if (!any)
            return null;

        return firstNegative is null
            ? CashflowSummary.UnlimitedRunway
            : Math.Max(0, firstNegative.Value.DayNumber - today.DayNumber - 1);
    }
}
