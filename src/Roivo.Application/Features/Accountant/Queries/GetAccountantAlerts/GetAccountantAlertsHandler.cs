using Roivo.Application.Abstractions;
using Roivo.Application.Features.Accountant.Queries.GetAccountantDashboard;
using Roivo.Application.Features.Accountant.Services;

namespace Roivo.Application.Features.Accountant.Queries.GetAccountantAlerts;

/// <summary>
/// Collects the attention-worthy conditions across every business the
/// signed-in accountant manages, worst first.
/// </summary>
/// <remarks>
/// Reads the stored projections in bulk for the same reason the dashboard does
/// — one query for the whole client book rather than a forecast run per
/// client.
/// </remarks>
public sealed class GetAccountantAlertsHandler
{
    /// <summary>Horizon the stored projection is scanned for a shortfall.</summary>
    public const int ForecastHorizonDays = 90;

    /// <summary>A tax deadline inside this many days raises a warning.</summary>
    public const int TaxDueWithinDays = 7;

    /// <summary>Below this match rate the client's paperwork is flagged.</summary>
    public const decimal LowMatchRateThreshold = 0.5m;

    private readonly IBusinessRepository _businesses;
    private readonly IReconciliationRepository _reconciliation;
    private readonly ICashflowRepository _cashflow;
    private readonly ITenantContext _tenant;
    private readonly IClock _clock;

    public GetAccountantAlertsHandler(
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

    public async Task<GetAccountantAlertsResult> Handle(
        GetAccountantAlertsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!AccountantAccess.CanViewAccountantWorkspace(_tenant.CurrentTenantType))
            return new GetAccountantAlertsResult.Forbidden("Ο χώρος εργασίας λογιστή είναι διαθέσιμος μόνο σε λογαριασμούς λογιστή.");

        // Tenant-filtered read: an accountant's alerts are about their own
        // clients, never about every tenant's.
        var businesses = await _businesses.ListActiveAsync(cancellationToken).ConfigureAwait(false);

        if (businesses.Count == 0)
            return new GetAccountantAlertsResult.Success([]);

        var today = _clock.Today;
        var ids = businesses.Select(b => b.Id).ToList();
        var names = businesses.ToDictionary(b => b.Id, b => b.Name);

        var forecasts = await _cashflow
            .ListStoredForecastsAsync(ids, today, today.AddDays(ForecastHorizonDays), cancellationToken)
            .ConfigureAwait(false);

        var obligations = await _cashflow
            .ListTaxObligationsForBusinessesAsync(
                ids, today, today.AddDays(TaxDueWithinDays), cancellationToken)
            .ConfigureAwait(false);

        var alerts = new List<AccountantAlert>();

        // The first day the projection dips below zero is the only one worth
        // reporting — every later negative day is the same shortfall.
        foreach (var group in forecasts.Where(f => f.ForecastDate > today).GroupBy(f => f.BusinessId))
        {
            var shortfall = group
                .Where(f => f.PredictedBalance < 0m)
                .OrderBy(f => f.ForecastDate)
                .FirstOrDefault();

            if (shortfall is null || !names.TryGetValue(group.Key, out var shortfallName))
                continue;

            alerts.Add(new AccountantAlert(
                BusinessId: group.Key,
                BusinessName: shortfallName,
                Type: AccountantAlertType.NegativeBalancePredicted,
                Severity: AccountantAlertSeverity.Critical,
                Date: shortfall.ForecastDate,
                Amount: shortfall.PredictedBalance));
        }

        foreach (var obligation in obligations.Where(o => !o.IsPaid))
        {
            if (!names.TryGetValue(obligation.BusinessId, out var taxName))
                continue;

            alerts.Add(new AccountantAlert(
                BusinessId: obligation.BusinessId,
                BusinessName: taxName,
                Type: AccountantAlertType.TaxDueSoon,
                Severity: AccountantAlertSeverity.Warning,
                Date: obligation.DueDate,
                Amount: obligation.ExpectedAmount));
        }

        foreach (var business in businesses)
        {
            // A non-zero streak always means "currently broken": any success
            // resets it to zero.
            if (business.BankingSyncErrorCount > 0)
            {
                alerts.Add(new AccountantAlert(
                    BusinessId: business.Id,
                    BusinessName: business.Name,
                    Type: AccountantAlertType.BankingSyncFailed,
                    Severity: AccountantAlertSeverity.Warning,
                    Date: business.BankingFirstFailureAt is { } at ? DateOnly.FromDateTime(at) : null,
                    Reason: business.BankingLastFailureReason));
            }

            if (business.HasAadeFailure)
            {
                alerts.Add(new AccountantAlert(
                    BusinessId: business.Id,
                    BusinessName: business.Name,
                    Type: AccountantAlertType.AadeSyncFailed,
                    Severity: AccountantAlertSeverity.Warning,
                    Date: business.AadeLastFailureAt is { } at ? DateOnly.FromDateTime(at) : null,
                    Reason: business.AadeLastFailureReason));
            }
        }

        var from = today.AddDays(-query.ReconciliationWindowDays);

        foreach (var business in businesses)
        {
            var counts = await _reconciliation
                .GetCountsAsync(business.Id, from, today, cancellationToken)
                .ConfigureAwait(false);

            // A client with no invoices in the window scores 0%, which is not
            // a backlog — it is an absence of work. Only flag real paperwork.
            if (counts.TotalInvoices == 0)
                continue;

            var rate = GetAccountantDashboardHandler.MatchRate(counts);
            if (rate >= LowMatchRateThreshold)
                continue;

            alerts.Add(new AccountantAlert(
                BusinessId: business.Id,
                BusinessName: business.Name,
                Type: AccountantAlertType.LowReconciliationRate,
                Severity: AccountantAlertSeverity.Info,
                Rate: rate));
        }

        IReadOnlyList<AccountantAlert> ordered =
        [
            .. alerts
                .OrderBy(a => AccountantAlertSeverity.Rank(a.Severity))
                .ThenBy(a => a.Date ?? DateOnly.MaxValue)
                .ThenBy(a => a.BusinessName, StringComparer.CurrentCulture)
                .ThenBy(a => a.Type, StringComparer.Ordinal)
        ];

        return new GetAccountantAlertsResult.Success(ordered);
    }
}
