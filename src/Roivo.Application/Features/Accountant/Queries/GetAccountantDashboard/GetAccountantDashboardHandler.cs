using Roivo.Application.Abstractions;
using Roivo.Application.Features.Accountant.Services;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Features.Accountant.Queries.GetAccountantDashboard;

/// <summary>
/// Builds the consolidated client list an accountant lands on.
/// </summary>
/// <remarks>
/// Cashflow and tax figures come from the bulk reads
/// (<see cref="ICashflowRepository.ListStoredForecastsAsync"/>,
/// <see cref="ICashflowRepository.ListTaxObligationsForBusinessesAsync"/>)
/// rather than from per-business forecast runs: an accountant with fifty
/// clients would otherwise pay for fifty ninety-day projections to render one
/// screen, for figures the nightly job has already persisted.
/// </remarks>
public sealed class GetAccountantDashboardHandler
{
    /// <summary>Horizon the stored projection is read over, matching the nightly run.</summary>
    public const int ForecastHorizonDays = 90;

    /// <summary>Window the "deadlines coming up" counts cover.</summary>
    public const int TaxDeadlineWindowDays = 30;

    private readonly IBusinessRepository _businesses;
    private readonly IReconciliationRepository _reconciliation;
    private readonly ICashflowRepository _cashflow;
    private readonly ITenantContext _tenant;
    private readonly IClock _clock;

    public GetAccountantDashboardHandler(
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

    public async Task<GetAccountantDashboardResult> Handle(
        GetAccountantDashboardQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!AccountantAccess.CanViewAccountantWorkspace(_tenant.CurrentTenantType))
            return new GetAccountantDashboardResult.Forbidden("Ο χώρος εργασίας λογιστή είναι διαθέσιμος μόνο σε λογαριασμούς λογιστή.");

        // Tenant-filtered by design: this is a request-scoped page, so the
        // accountant's own client book is exactly the right set. The
        // ...AcrossAllTenantsAsync variants exist for crons and would leak
        // other accountants' clients into this view.
        var businesses = await _businesses.ListActiveAsync(cancellationToken).ConfigureAwait(false);

        if (businesses.Count == 0)
        {
            return new GetAccountantDashboardResult.Success(new AccountantDashboard(
                Summary: new AccountantDashboardSummary(0, 0m, 0, 0),
                Businesses: []));
        }

        var today = _clock.Today;
        var ids = businesses.Select(b => b.Id).ToList();

        var forecastsByBusiness = (await _cashflow
                .ListStoredForecastsAsync(ids, today, today.AddDays(ForecastHorizonDays), cancellationToken)
                .ConfigureAwait(false))
            .GroupBy(f => f.BusinessId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<CashflowForecast>)[.. g]);

        var deadlinesByBusiness = (await _cashflow
                .ListTaxObligationsForBusinessesAsync(
                    ids, today, today.AddDays(TaxDeadlineWindowDays), cancellationToken)
                .ConfigureAwait(false))
            .Where(o => !o.IsPaid)
            .GroupBy(o => o.BusinessId)
            .ToDictionary(g => g.Key, g => g.Count());

        var from = today.AddDays(-query.ReconciliationWindowDays);
        var rows = new List<AccountantBusinessRow>(businesses.Count);

        foreach (var business in businesses)
        {
            var counts = await _reconciliation
                .GetCountsAsync(business.Id, from, today, cancellationToken)
                .ConfigureAwait(false);

            var runway = forecastsByBusiness.TryGetValue(business.Id, out var forecasts)
                ? CashflowHealth.RunwayFromStoredForecasts(forecasts, today)
                : null;

            rows.Add(new AccountantBusinessRow(
                BusinessId: business.Id,
                Name: business.Name,
                Afm: business.Afm,
                AadeConnected: IsAadeConnected(business),
                BankingConnected: IsBankingConnected(business),
                TotalInvoices: counts.TotalInvoices,
                ConfirmedMatches: counts.ConfirmedMatches,
                UnreconciledInvoices: counts.UnreconciledInvoices,
                MatchRate: MatchRate(counts),
                CashflowHealth: CashflowHealth.FromDaysOfRunway(runway),
                DaysOfRunway: runway,
                TaxDeadlinesNext30Days: deadlinesByBusiness.GetValueOrDefault(business.Id),
                LastAadeSyncAt: business.LastAadeSyncAt,
                LastBankingSyncAt: business.LastBankingSyncAt));
        }

        return new GetAccountantDashboardResult.Success(new AccountantDashboard(
            Summary: new AccountantDashboardSummary(
                TotalBusinesses: rows.Count,
                // rows is non-empty here, but the divide stays explicit so a
                // future caller cannot reintroduce a NaN average.
                AverageMatchRate: rows.Count == 0
                    ? 0m
                    : Math.Round(rows.Sum(r => r.MatchRate) / rows.Count, 4),
                BusinessesWithCashflowWarnings: rows.Count(r => CashflowHealth.IsWarning(r.CashflowHealth)),
                TaxDeadlinesNext30Days: rows.Sum(r => r.TaxDeadlinesNext30Days)),
            Businesses: rows));
    }

    /// <summary>
    /// Confirmed matches against invoices seen in the window — the same
    /// denominator the single-business dashboard uses, so the figures agree.
    /// Zero invoices reads as 0%, never as a division by zero.
    /// </summary>
    internal static decimal MatchRate(ReconciliationCounts counts)
        => counts.TotalInvoices == 0
            ? 0m
            : Math.Round((decimal)counts.ConfirmedMatches / counts.TotalInvoices, 4);

    internal static bool IsAadeConnected(Business business)
        => !string.IsNullOrEmpty(business.AadeUserIdEncrypted)
        && !string.IsNullOrEmpty(business.AadeSubscriptionKeyEncrypted);

    internal static bool IsBankingConnected(Business business)
        => !string.IsNullOrEmpty(business.BankingAccessTokenEncrypted);
}
