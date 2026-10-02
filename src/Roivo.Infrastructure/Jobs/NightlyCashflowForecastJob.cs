using Microsoft.Extensions.Logging;
using Roivo.Application.Abstractions;
using Roivo.Application.Features.Cashflow.Services;
using Roivo.Core.Domain.Entities;

namespace Roivo.Infrastructure.Jobs;

/// <summary>
/// Hangfire-invoked recurring job. Stores a 90-day projection for every
/// bank-connected business with enough history. Runs at 06:00 Europe/Athens,
/// after the night's reconciliation, so the forecast is built on matched data.
/// </summary>
public sealed class NightlyCashflowForecastJob
{
    public const int ForecastDays = 90;

    private readonly IBusinessRepository _businesses;
    private readonly ICashflowRepository _cashflow;
    private readonly ICashflowForecastEngine _engine;
    private readonly IGreekTaxCalendar _taxCalendar;
    private readonly ILogger<NightlyCashflowForecastJob> _logger;

    public NightlyCashflowForecastJob(
        IBusinessRepository businesses,
        ICashflowRepository cashflow,
        ICashflowForecastEngine engine,
        IGreekTaxCalendar taxCalendar,
        ILogger<NightlyCashflowForecastJob> logger)
    {
        ArgumentNullException.ThrowIfNull(businesses);
        ArgumentNullException.ThrowIfNull(cashflow);
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(taxCalendar);
        ArgumentNullException.ThrowIfNull(logger);

        _businesses = businesses;
        _cashflow = cashflow;
        _engine = engine;
        _taxCalendar = taxCalendar;
        _logger = logger;
    }

    // Hangfire requires a public, parameterless entry point.
    public async Task Execute()
    {
        var businessIds = await _businesses
            .ListIdsWithBankingCredentialsAcrossAllTenantsAsync()
            .ConfigureAwait(false);

        _logger.LogInformation("Starting nightly cashflow forecast for {Count} businesses", businessIds.Count);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var horizon = today.AddDays(ForecastDays);

        foreach (var businessId in businessIds)
        {
            var earliest = await _cashflow.GetEarliestTransactionDateAsync(businessId).ConfigureAwait(false);
            var historyDays = earliest is null ? 0 : today.DayNumber - earliest.Value.DayNumber;

            if (historyDays < CashflowForecastEngine.MinimumHistoryDays)
            {
                _logger.LogDebug(
                    "Skipping business {BusinessId}: {Days} days of history, {Required} required",
                    businessId, historyDays, CashflowForecastEngine.MinimumHistoryDays);
                continue;
            }

            // Refresh the calendar first so the projection subtracts obligations
            // that came due since the last run.
            var obligations = await _taxCalendar.GenerateAsync(businessId, today, horizon).ConfigureAwait(false);
            if (obligations.Count > 0)
                await _cashflow.UpsertTaxObligationsAsync(obligations).ConfigureAwait(false);

            var forecast = await _engine.ForecastAsync(businessId, ForecastDays).ConfigureAwait(false);
            if (forecast.DailyForecasts.Count == 0)
                continue;

            var rows = forecast.DailyForecasts
                .Select(d => CashflowForecast.Create(
                    businessId, d.Date, d.PredictedInflow, d.PredictedOutflow,
                    d.PredictedBalance, d.ConfidenceLow, d.ConfidenceHigh))
                .ToList();

            await _cashflow.ReplaceForecastsAsync(businessId, rows).ConfigureAwait(false);

            var critical = forecast.Alerts
                .Count(a => a.Severity == CashflowAlertSeverity.Critical);

            // Alerts are logged, not emailed. Wiring these into EmailJob needs a
            // dedupe stamp per business so a persistent shortfall does not mail
            // the owner nightly; see PROGRESS.md.
            if (critical > 0)
            {
                _logger.LogWarning(
                    "Business {BusinessId} forecast has {Count} critical alerts; runway {Runway} days",
                    businessId, critical, forecast.Summary.DaysOfRunway);
            }

            _logger.LogInformation(
                "Stored {Days}-day forecast for business {BusinessId}", rows.Count, businessId);
        }
    }
}
