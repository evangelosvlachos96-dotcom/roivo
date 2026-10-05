using Roivo.Application.Abstractions;
using Roivo.Application.Features.Accountant.Queries.GetAccountantDashboard;
using Roivo.Application.Features.Accountant.Services;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Features.Accountant.Queries.GetAccountantReport;

/// <summary>
/// Builds the consolidated month-end report: reconciliation per client, one
/// combined tax calendar, and the clients' projections side by side.
/// </summary>
/// <remarks>
/// Tax and cashflow figures come from the bulk reads, so the report costs two
/// queries for those regardless of how many clients the accountant has.
/// </remarks>
public sealed class GetAccountantReportHandler
{
    /// <summary>How far ahead the combined tax calendar looks.</summary>
    /// <summary>How far back overdue obligations stay visible.</summary>
    private const int OverdueLookbackDays = 90;

    public const int TaxCalendarHorizonDays = 180;

    /// <summary>Horizon the stored projection is read over.</summary>
    public const int ForecastHorizonDays = 90;

    private readonly IBusinessRepository _businesses;
    private readonly IReconciliationRepository _reconciliation;
    private readonly ICashflowRepository _cashflow;
    private readonly ITenantContext _tenant;
    private readonly IClock _clock;

    public GetAccountantReportHandler(
        IBusinessRepository businesses,
        IReconciliationRepository reconciliation,
        ICashflowRepository cashflow,
        ITenantContext tenant,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(businesses);
        ArgumentNullException.ThrowIfNull(reconciliation);
        ArgumentNullException.ThrowIfNull(cashflow);
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentNullException.ThrowIfNull(clock);

        _businesses = businesses;
        _reconciliation = reconciliation;
        _cashflow = cashflow;
        _tenant = tenant;
        _clock = clock;
    }

    public async Task<GetAccountantReportResult> Handle(
        GetAccountantReportQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!AccountantAccess.CanViewAccountantWorkspace(_tenant.CurrentTenantType))
            return new GetAccountantReportResult.Forbidden("Ο χώρος εργασίας λογιστή είναι διαθέσιμος μόνο σε λογαριασμούς λογιστή.");

        var today = _clock.Today;
        var anchor = query.Month ?? today;
        var from = new DateOnly(anchor.Year, anchor.Month, 1);
        var to = from.AddMonths(1).AddDays(-1);
        var taxCalendarTo = today.AddDays(TaxCalendarHorizonDays);

        // Tenant-filtered read: the report consolidates this accountant's own
        // client book, never every tenant's.
        var businesses = await _businesses.ListActiveAsync(cancellationToken).ConfigureAwait(false);

        if (businesses.Count == 0)
        {
            return new GetAccountantReportResult.Success(new AccountantReport(
                From: from,
                To: to,
                TaxCalendarTo: taxCalendarTo,
                ReconciliationSummary: [],
                ReconciliationTotals: new AccountantReconciliationTotals(0, 0, 0, 0, 0, 0, 0m, 0m),
                TaxCalendar: [],
                CashflowOverview: []));
        }

        var ids = businesses.Select(b => b.Id).ToList();
        var names = businesses.ToDictionary(b => b.Id, b => b.Name);

        var obligations = await _cashflow
            // Starts before today so an obligation that is already late still
            // appears. With a floor of today, TaxObligation.IsOverdue below
            // could never be true and the overdue status was unreachable.
            .ListTaxObligationsForBusinessesAsync(
                ids, today.AddDays(-OverdueLookbackDays), taxCalendarTo, cancellationToken)
            .ConfigureAwait(false);

        var forecastsByBusiness = (await _cashflow
                .ListStoredForecastsAsync(ids, today, today.AddDays(ForecastHorizonDays), cancellationToken)
                .ConfigureAwait(false))
            .GroupBy(f => f.BusinessId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<CashflowForecast>)[.. g]);

        var countsByBusiness = await _reconciliation
            .GetCountsForBusinessesAsync(ids, from, to, cancellationToken)
            .ConfigureAwait(false);

        var reconciliation = new List<AccountantReconciliationRow>(businesses.Count);

        foreach (var business in businesses)
        {
            var counts = countsByBusiness[business.Id];

            reconciliation.Add(new AccountantReconciliationRow(
                BusinessId: business.Id,
                BusinessName: business.Name,
                Afm: business.Afm,
                TotalInvoices: counts.TotalInvoices,
                TotalTransactions: counts.TotalTransactions,
                ConfirmedMatches: counts.ConfirmedMatches,
                PendingMatches: counts.PendingMatches,
                UnreconciledInvoices: counts.UnreconciledInvoices,
                UnreconciledTransactions: counts.UnreconciledTransactions,
                ReconciledAmount: counts.ReconciledAmount,
                MatchRate: GetAccountantDashboardHandler.MatchRate(counts)));
        }

        IReadOnlyList<AccountantTaxCalendarRow> taxCalendar =
        [
            .. obligations
                .Where(o => names.ContainsKey(o.BusinessId))
                .OrderBy(o => o.DueDate)
                .ThenBy(o => names[o.BusinessId], StringComparer.CurrentCulture)
                .Select(o => new AccountantTaxCalendarRow(
                    BusinessId: o.BusinessId,
                    BusinessName: names[o.BusinessId],
                    TaxType: o.TaxType.ToString(),
                    Period: o.Period,
                    DueDate: o.DueDate,
                    ExpectedAmount: o.ExpectedAmount,
                    IsPaid: o.IsPaid,
                    IsOverdue: o.IsOverdue(today)))
        ];

        var cashflow = new List<AccountantCashflowRow>(businesses.Count);

        foreach (var business in businesses)
        {
            forecastsByBusiness.TryGetValue(business.Id, out var stored);
            IReadOnlyList<CashflowForecast> forecasts = stored ?? [];
            var runway = CashflowHealth.RunwayFromStoredForecasts(forecasts, today);

            cashflow.Add(new AccountantCashflowRow(
                BusinessId: business.Id,
                BusinessName: business.Name,
                Health: CashflowHealth.FromDaysOfRunway(runway),
                DaysOfRunway: runway,
                Balance30Day: BalanceAt(forecasts, today.AddDays(30)),
                Balance60Day: BalanceAt(forecasts, today.AddDays(60)),
                Balance90Day: BalanceAt(forecasts, today.AddDays(90)),
                PredictedInflow: forecasts.Where(f => f.ForecastDate > today).Sum(f => f.PredictedInflow),
                PredictedOutflow: forecasts.Where(f => f.ForecastDate > today).Sum(f => f.PredictedOutflow)));
        }

        return new GetAccountantReportResult.Success(new AccountantReport(
            From: from,
            To: to,
            TaxCalendarTo: taxCalendarTo,
            ReconciliationSummary: reconciliation,
            ReconciliationTotals: new AccountantReconciliationTotals(
                Businesses: reconciliation.Count,
                TotalInvoices: reconciliation.Sum(r => r.TotalInvoices),
                TotalTransactions: reconciliation.Sum(r => r.TotalTransactions),
                ConfirmedMatches: reconciliation.Sum(r => r.ConfirmedMatches),
                PendingMatches: reconciliation.Sum(r => r.PendingMatches),
                UnreconciledInvoices: reconciliation.Sum(r => r.UnreconciledInvoices),
                ReconciledAmount: reconciliation.Sum(r => r.ReconciledAmount),
                AverageMatchRate: reconciliation.Count == 0
                    ? 0m
                    : Math.Round(reconciliation.Sum(r => r.MatchRate) / reconciliation.Count, 4)),
            TaxCalendar: taxCalendar,
            CashflowOverview: cashflow));
    }

    /// <summary>
    /// Projected closing balance on a given day, or null when that day is not
    /// in the stored projection — reporting zero would read as "broke".
    /// </summary>
    private static decimal? BalanceAt(IReadOnlyList<CashflowForecast> forecasts, DateOnly date)
        => forecasts.FirstOrDefault(f => f.ForecastDate == date)?.PredictedBalance;
}
