using Roivo.Application.Features.Cashflow.Services;

namespace Roivo.Application.Features.Cashflow.Queries.GetCashflowForecast;

public sealed class GetCashflowForecastHandler
{
    private readonly ICashflowForecastEngine _engine;

    public GetCashflowForecastHandler(ICashflowForecastEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);
        _engine = engine;
    }

    public async Task<CashflowForecastResult> Handle(
        GetCashflowForecastQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await _engine
            .ForecastAsync(query.BusinessId, query.DaysAhead, cancellationToken)
            .ConfigureAwait(false);
    }
}
