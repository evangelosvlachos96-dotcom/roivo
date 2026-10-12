namespace Roivo.Application.Features.Cashflow.Queries.GetCashflowForecast;

public sealed record GetCashflowForecastQuery(Guid BusinessId, int DaysAhead = 90);
