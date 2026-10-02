using Roivo.Core.Domain.Exceptions;
using Roivo.Core.Domain.Interfaces;

namespace Roivo.Core.Domain.Entities;

/// <summary>
/// One day of a stored cashflow projection, with the actuals backfilled once
/// that day has passed.
/// </summary>
/// <remarks>
/// Forecasts are persisted rather than recomputed on read so that prediction
/// quality can be measured: keeping the prediction alongside what actually
/// happened is the only way to tell whether the engine is getting better.
/// </remarks>
public class CashflowForecast : ITenantScoped
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }

    public Guid BusinessId { get; private set; }
    public Business? Business { get; private set; }

    public DateOnly ForecastDate { get; private set; }

    public decimal PredictedInflow { get; private set; }
    public decimal PredictedOutflow { get; private set; }
    public decimal PredictedBalance { get; private set; }

    /// <summary>Lower bound of the prediction band. Widens with distance from today.</summary>
    public decimal ConfidenceLow { get; private set; }

    /// <summary>Upper bound of the prediction band.</summary>
    public decimal ConfidenceHigh { get; private set; }

    public decimal? ActualInflow { get; private set; }
    public decimal? ActualOutflow { get; private set; }
    public decimal? ActualBalance { get; private set; }

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Required for EF Core materialization. Use CashflowForecast.Create(...).
    private CashflowForecast() { }

    /// <exception cref="DomainException">The confidence band is inverted.</exception>
    public static CashflowForecast Create(
        Guid businessId,
        DateOnly forecastDate,
        decimal predictedInflow,
        decimal predictedOutflow,
        decimal predictedBalance,
        decimal confidenceLow,
        decimal confidenceHigh)
    {
        if (confidenceLow > confidenceHigh)
            throw new DomainException("Confidence low bound cannot exceed the high bound.");

        return new CashflowForecast
        {
            BusinessId = businessId,
            ForecastDate = forecastDate,
            PredictedInflow = predictedInflow,
            PredictedOutflow = predictedOutflow,
            PredictedBalance = predictedBalance,
            ConfidenceLow = confidenceLow,
            ConfidenceHigh = confidenceHigh,
        };
    }

    /// <summary>Backfills what actually happened on <see cref="ForecastDate"/>.</summary>
    public void RecordActuals(decimal actualInflow, decimal actualOutflow, decimal actualBalance)
    {
        ActualInflow = actualInflow;
        ActualOutflow = actualOutflow;
        ActualBalance = actualBalance;
    }

    /// <summary>
    /// Signed difference between the predicted and actual closing balance, or
    /// null while the day is still in the future.
    /// </summary>
    public decimal? BalanceError => ActualBalance is null ? null : PredictedBalance - ActualBalance;
}
