using Roivo.Application.Abstractions;
using Roivo.Application.Features.Cashflow.Services;

namespace Roivo.Application.Features.Cashflow.Queries.GetCashflowDashboard;

public sealed class GetCashflowDashboardHandler
{
    private readonly ICashflowForecastEngine _engine;
    private readonly ICashflowRepository _repository;

    public GetCashflowDashboardHandler(ICashflowForecastEngine engine, ICashflowRepository repository)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(repository);

        _engine = engine;
        _repository = repository;
    }

    public async Task<CashflowDashboard> Handle(
        GetCashflowDashboardQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var earliest = await _repository
            .GetEarliestTransactionDateAsync(query.BusinessId, cancellationToken)
            .ConfigureAwait(false);

        var historyDays = earliest is null
            ? 0
            : DateOnly.FromDateTime(DateTime.UtcNow).DayNumber - earliest.Value.DayNumber;

        var hasHistory = historyDays >= CashflowForecastEngine.MinimumHistoryDays;

        var forecast = await _engine
            .ForecastAsync(query.BusinessId, query.DaysAhead, cancellationToken)
            .ConfigureAwait(false);

        return new CashflowDashboard(
            BusinessId: query.BusinessId,
            HasSufficientHistory: hasHistory,
            Summary: forecast.Summary,
            DailyForecasts: forecast.DailyForecasts,
            UpcomingTaxObligations: forecast.UpcomingTaxObligations,
            Alerts: forecast.Alerts);
    }
}
