using FluentAssertions;
using Roivo.Application.Features.Accountant.Queries.GetAccountantAlerts;
using Roivo.Application.Features.Accountant.Queries.GetAccountantDashboard;
using Roivo.Application.Features.Accountant.Queries.GetAccountantReport;
using Roivo.Application.Features.Accountant.Services;
using Roivo.Application.Tests.Fakes;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Tests.Features.Accountant;

/// <summary>Application-layer handler tests for the M9 accountant workspace.</summary>
public class AccountantHandlerTests
{
    // Well-formed AFMs that pass the Mod 11 check used by AfmValidator.
    private const string ValidAfm = "094014201";
    private const string AnotherValidAfm = "123456783";

    private static readonly DateOnly Today = new(2026, 6, 15);

    private sealed record Sut(
        GetAccountantDashboardHandler Dashboard,
        GetAccountantAlertsHandler Alerts,
        GetAccountantReportHandler Report,
        FakeBusinessRepository Businesses,
        FakeReconciliationRepository Reconciliation,
        FakeCashflowRepository Cashflow,
        FakeTenantContext Tenant,
        FakeClock Clock);

    private static Sut BuildSut(TenantType tenantType = TenantType.Accountant)
    {
        var businesses = new FakeBusinessRepository();
        var reconciliation = new FakeReconciliationRepository();
        var cashflow = new FakeCashflowRepository();
        var tenant = new FakeTenantContext { CurrentTenantType = tenantType };
        var clock = new FakeClock(Today);

        return new Sut(
            new GetAccountantDashboardHandler(businesses, reconciliation, cashflow, tenant, clock),
            new GetAccountantAlertsHandler(businesses, reconciliation, cashflow, tenant, clock),
            new GetAccountantReportHandler(businesses, reconciliation, cashflow, tenant, clock),
            businesses,
            reconciliation,
            cashflow,
            tenant,
            clock);
    }

    private static Business Seed(Sut sut, string name, string afm = ValidAfm)
    {
        var business = Business.Create(name, afm, null, null);
        business.TenantId = sut.Tenant.CurrentTenantId;
        sut.Businesses.Store[business.Id] = business;
        return business;
    }

    private static Invoice SeedInvoice(Sut sut, Guid businessId, decimal gross = 1000m)
    {
        var invoice = Invoice.Create(
            businessId,
            aadeMark: Guid.NewGuid().ToString(),
            direction: InvoiceDirection.Issued,
            invoiceType: "1.1",
            issueDate: Today.AddDays(-5),
            counterpartyAfm: AnotherValidAfm,
            counterpartyName: "ACME AE",
            netAmount: gross,
            vatAmount: 0m,
            grossAmount: gross,
            currency: Currency.EUR,
            cancelledByMark: null);

        sut.Reconciliation.Invoices.Add(invoice);
        return invoice;
    }

    private static void SeedForecast(Sut sut, Guid businessId, int dayOffset, decimal balance)
        => sut.Cashflow.StoredForecasts.Add(CashflowForecast.Create(
            businessId,
            Today.AddDays(dayOffset),
            predictedInflow: 100m,
            predictedOutflow: 100m,
            predictedBalance: balance,
            confidenceLow: balance - 100m,
            confidenceHigh: balance + 100m));

    private static void SeedObligation(Sut sut, Guid businessId, int dayOffset, decimal amount = 500m)
        => sut.Cashflow.TaxObligations.Add(TaxObligation.Create(
            businessId,
            TaxType.Vat,
            Today.AddDays(dayOffset),
            amount,
            period: $"2026-{dayOffset:00}"));

    // ---------------------------------------------------------------- permissions

    [Fact]
    public async Task Dashboard_forbids_a_business_tenant()
    {
        var sut = BuildSut(TenantType.Business);

        var result = await sut.Dashboard.Handle(new GetAccountantDashboardQuery());

        result.Should().BeOfType<GetAccountantDashboardResult.Forbidden>();
    }

    [Fact]
    public async Task Alerts_forbid_a_business_tenant()
    {
        var sut = BuildSut(TenantType.Business);

        var result = await sut.Alerts.Handle(new GetAccountantAlertsQuery());

        result.Should().BeOfType<GetAccountantAlertsResult.Forbidden>();
    }

    [Fact]
    public async Task Report_forbids_a_business_tenant()
    {
        var sut = BuildSut(TenantType.Business);

        var result = await sut.Report.Handle(new GetAccountantReportQuery());

        result.Should().BeOfType<GetAccountantReportResult.Forbidden>();
    }

    [Fact]
    public async Task Permission_check_runs_before_any_repository_read()
    {
        var sut = BuildSut(TenantType.Business);
        Seed(sut, "Acme");

        await sut.Dashboard.Handle(new GetAccountantDashboardQuery());

        // Nothing was asked of the cashflow repository: a tenant with no
        // workspace should not cost a query.
        sut.Cashflow.VatTotalsRequests.Should().BeEmpty();
    }

    // --------------------------------------------------------------- empty state

    [Fact]
    public async Task Dashboard_with_no_businesses_returns_a_zeroed_summary()
    {
        var sut = BuildSut();

        var result = await sut.Dashboard.Handle(new GetAccountantDashboardQuery());

        var dashboard = result.Should().BeOfType<GetAccountantDashboardResult.Success>()
            .Which.Dashboard;

        dashboard.Businesses.Should().BeEmpty();
        dashboard.Summary.TotalBusinesses.Should().Be(0);
        dashboard.Summary.BusinessesWithCashflowWarnings.Should().Be(0);
        dashboard.Summary.TaxDeadlinesNext30Days.Should().Be(0);

        // The divide-by-zero case: an average over nothing is zero, not NaN.
        dashboard.Summary.AverageMatchRate.Should().Be(0m);
    }

    [Fact]
    public async Task Dashboard_average_match_rate_is_zero_when_no_business_has_invoices()
    {
        var sut = BuildSut();
        Seed(sut, "Acme");
        Seed(sut, "Beta", AnotherValidAfm);

        var result = await sut.Dashboard.Handle(new GetAccountantDashboardQuery());

        var dashboard = result.Should().BeOfType<GetAccountantDashboardResult.Success>()
            .Which.Dashboard;

        dashboard.Summary.TotalBusinesses.Should().Be(2);
        dashboard.Summary.AverageMatchRate.Should().Be(0m);
        dashboard.Businesses.Should().OnlyContain(b => b.MatchRate == 0m);
    }

    [Fact]
    public async Task Alerts_with_no_businesses_returns_an_empty_list()
    {
        var sut = BuildSut();

        var result = await sut.Alerts.Handle(new GetAccountantAlertsQuery());

        result.Should().BeOfType<GetAccountantAlertsResult.Success>()
            .Which.Alerts.Should().BeEmpty();
    }

    [Fact]
    public async Task Report_with_no_businesses_returns_zeroed_totals()
    {
        var sut = BuildSut();

        var result = await sut.Report.Handle(new GetAccountantReportQuery());

        var report = result.Should().BeOfType<GetAccountantReportResult.Success>().Which.Report;

        report.ReconciliationSummary.Should().BeEmpty();
        report.TaxCalendar.Should().BeEmpty();
        report.CashflowOverview.Should().BeEmpty();
        report.ReconciliationTotals.Businesses.Should().Be(0);
        report.ReconciliationTotals.AverageMatchRate.Should().Be(0m);
        report.From.Should().Be(new DateOnly(2026, 6, 1));
        report.To.Should().Be(new DateOnly(2026, 6, 30));
    }

    // ------------------------------------------------------------ dashboard rows

    [Fact]
    public async Task Dashboard_reports_connection_state_and_last_sync()
    {
        var sut = BuildSut();
        var business = Seed(sut, "Acme");
        business.AadeUserIdEncrypted = "cipher";
        business.AadeSubscriptionKeyEncrypted = "cipher";
        business.RecordAadeSync(new DateTime(2026, 6, 14, 3, 0, 0, DateTimeKind.Utc));

        var result = await sut.Dashboard.Handle(new GetAccountantDashboardQuery());

        var row = result.Should().BeOfType<GetAccountantDashboardResult.Success>()
            .Which.Dashboard.Businesses.Should().ContainSingle().Subject;

        row.Name.Should().Be("Acme");
        row.Afm.Should().Be(ValidAfm);
        row.AadeConnected.Should().BeTrue();
        row.BankingConnected.Should().BeFalse();
        row.LastAadeSyncAt.Should().Be(new DateTime(2026, 6, 14, 3, 0, 0, DateTimeKind.Utc));
        row.LastBankingSyncAt.Should().BeNull();
    }

    [Fact]
    public async Task Dashboard_counts_cashflow_warnings_and_near_deadlines()
    {
        var sut = BuildSut();
        var warning = Seed(sut, "Thin Margins");
        var healthy = Seed(sut, "Comfortable", AnotherValidAfm);

        // Dips negative on day 40: solvent today, dry inside the horizon.
        for (var day = 1; day <= 90; day++)
            SeedForecast(sut, warning.Id, day, day >= 40 ? -500m : 1_000m);

        for (var day = 1; day <= 90; day++)
            SeedForecast(sut, healthy.Id, day, 10_000m);

        SeedObligation(sut, warning.Id, dayOffset: 10);
        SeedObligation(sut, healthy.Id, dayOffset: 45);

        var result = await sut.Dashboard.Handle(new GetAccountantDashboardQuery());

        var dashboard = result.Should().BeOfType<GetAccountantDashboardResult.Success>()
            .Which.Dashboard;

        dashboard.Summary.BusinessesWithCashflowWarnings.Should().Be(1);

        // Only the obligation inside thirty days counts.
        dashboard.Summary.TaxDeadlinesNext30Days.Should().Be(1);

        dashboard.Businesses.Single(b => b.BusinessId == warning.Id)
            .CashflowHealth.Should().Be(CashflowHealth.Yellow);
        dashboard.Businesses.Single(b => b.BusinessId == healthy.Id)
            .CashflowHealth.Should().Be(CashflowHealth.Green);
    }

    [Fact]
    public async Task Dashboard_reports_unknown_health_for_a_business_with_no_stored_forecast()
    {
        var sut = BuildSut();
        Seed(sut, "Never Forecast");

        var result = await sut.Dashboard.Handle(new GetAccountantDashboardQuery());

        var row = result.Should().BeOfType<GetAccountantDashboardResult.Success>()
            .Which.Dashboard.Businesses.Should().ContainSingle().Subject;

        row.CashflowHealth.Should().Be(CashflowHealth.Unknown);
        row.DaysOfRunway.Should().BeNull();
    }

    // ---------------------------------------------------------------- alerts

    [Fact]
    public async Task Alerts_are_ordered_critical_then_warning_then_info()
    {
        var sut = BuildSut();
        var sinking = Seed(sut, "Sinking");
        var taxed = Seed(sut, "Taxed", AnotherValidAfm);
        var behind = Seed(sut, "Behind", "090000045");

        SeedForecast(sut, sinking.Id, dayOffset: 5, balance: -2_000m);
        SeedObligation(sut, taxed.Id, dayOffset: 3);

        // One invoice, no confirmed match: a 0% rate on real paperwork.
        SeedInvoice(sut, behind.Id);

        var result = await sut.Alerts.Handle(new GetAccountantAlertsQuery());

        var alerts = result.Should().BeOfType<GetAccountantAlertsResult.Success>().Which.Alerts;

        alerts.Select(a => a.Severity).Should().Equal(
            AccountantAlertSeverity.Critical,
            AccountantAlertSeverity.Warning,
            AccountantAlertSeverity.Info);

        alerts.Select(a => a.Type).Should().Equal(
            AccountantAlertType.NegativeBalancePredicted,
            AccountantAlertType.TaxDueSoon,
            AccountantAlertType.LowReconciliationRate);

        // Every alert names its business so the page can deep-link.
        alerts.Select(a => a.BusinessId).Should().Equal(sinking.Id, taxed.Id, behind.Id);
        alerts.Select(a => a.BusinessName).Should().Equal("Sinking", "Taxed", "Behind");
    }

    [Fact]
    public async Task Alerts_order_sync_failures_above_a_low_match_rate()
    {
        var sut = BuildSut();
        var broken = Seed(sut, "Broken Feed");
        broken.AadeUserIdEncrypted = "cipher";
        broken.AadeSubscriptionKeyEncrypted = "cipher";
        broken.RecordAadeSyncFailure("InvalidCredentials");
        broken.RecordBankingSyncFailure("SessionExpired");

        var behind = Seed(sut, "Behind", AnotherValidAfm);
        SeedInvoice(sut, behind.Id);

        var result = await sut.Alerts.Handle(new GetAccountantAlertsQuery());

        var alerts = result.Should().BeOfType<GetAccountantAlertsResult.Success>().Which.Alerts;

        alerts.Select(a => a.Severity).Should().Equal(
            AccountantAlertSeverity.Warning,
            AccountantAlertSeverity.Warning,
            AccountantAlertSeverity.Info);

        alerts.Where(a => a.Type == AccountantAlertType.AadeSyncFailed)
            .Should().ContainSingle().Which.Reason.Should().Be("InvalidCredentials");

        alerts.Where(a => a.Type == AccountantAlertType.BankingSyncFailed)
            .Should().ContainSingle().Which.Reason.Should().Be("SessionExpired");
    }

    [Fact]
    public async Task Alerts_report_only_the_first_predicted_shortfall_per_business()
    {
        var sut = BuildSut();
        var sinking = Seed(sut, "Sinking");

        SeedForecast(sut, sinking.Id, dayOffset: 20, balance: -100m);
        SeedForecast(sut, sinking.Id, dayOffset: 10, balance: -50m);
        SeedForecast(sut, sinking.Id, dayOffset: 30, balance: -900m);

        var result = await sut.Alerts.Handle(new GetAccountantAlertsQuery());

        var alert = result.Should().BeOfType<GetAccountantAlertsResult.Success>()
            .Which.Alerts.Should().ContainSingle().Subject;

        alert.Type.Should().Be(AccountantAlertType.NegativeBalancePredicted);
        alert.Date.Should().Be(Today.AddDays(10));
        alert.Amount.Should().Be(-50m);
    }

    [Fact]
    public async Task Alerts_skip_a_tax_obligation_already_paid()
    {
        var sut = BuildSut();
        var business = Seed(sut, "Paid Up");
        SeedObligation(sut, business.Id, dayOffset: 2);
        sut.Cashflow.TaxObligations[0].MarkPaid(500m, sut.Clock.UtcNow);

        var result = await sut.Alerts.Handle(new GetAccountantAlertsQuery());

        result.Should().BeOfType<GetAccountantAlertsResult.Success>()
            .Which.Alerts.Should().BeEmpty();
    }

    [Fact]
    public async Task Alerts_do_not_flag_a_business_with_no_invoices_in_the_window()
    {
        var sut = BuildSut();
        Seed(sut, "Dormant");

        var result = await sut.Alerts.Handle(new GetAccountantAlertsQuery());

        // Zero invoices scores 0%, but that is an absence of work rather than a
        // backlog, so it must not raise the reconciliation alert.
        result.Should().BeOfType<GetAccountantAlertsResult.Success>()
            .Which.Alerts.Should().BeEmpty();
    }

    // ---------------------------------------------------------------- report

    [Fact]
    public async Task Report_consolidates_all_three_views()
    {
        var sut = BuildSut();
        var acme = Seed(sut, "Acme");
        var beta = Seed(sut, "Beta", AnotherValidAfm);

        SeedInvoice(sut, acme.Id, gross: 2_000m);
        SeedObligation(sut, beta.Id, dayOffset: 20);

        for (var day = 1; day <= 90; day++)
            SeedForecast(sut, acme.Id, day, 5_000m);

        var result = await sut.Report.Handle(new GetAccountantReportQuery());

        var report = result.Should().BeOfType<GetAccountantReportResult.Success>().Which.Report;

        report.ReconciliationSummary.Should().HaveCount(2);
        report.ReconciliationTotals.Businesses.Should().Be(2);
        report.ReconciliationTotals.TotalInvoices.Should().Be(1);

        report.TaxCalendar.Should().ContainSingle()
            .Which.BusinessName.Should().Be("Beta");

        report.CashflowOverview.Should().HaveCount(2);
        report.CashflowOverview.Single(r => r.BusinessId == acme.Id)
            .Health.Should().Be(CashflowHealth.Green);
        report.CashflowOverview.Single(r => r.BusinessId == acme.Id)
            .Balance90Day.Should().Be(5_000m);
        report.CashflowOverview.Single(r => r.BusinessId == beta.Id)
            .Health.Should().Be(CashflowHealth.Unknown);
    }

    [Fact]
    public async Task Report_month_defaults_to_the_clock_and_honours_an_explicit_month()
    {
        var sut = BuildSut();

        var current = await sut.Report.Handle(new GetAccountantReportQuery());
        current.Should().BeOfType<GetAccountantReportResult.Success>()
            .Which.Report.From.Should().Be(new DateOnly(2026, 6, 1));

        var february = await sut.Report.Handle(
            new GetAccountantReportQuery(new DateOnly(2026, 2, 20)));

        var report = february.Should().BeOfType<GetAccountantReportResult.Success>().Which.Report;
        report.From.Should().Be(new DateOnly(2026, 2, 1));
        report.To.Should().Be(new DateOnly(2026, 2, 28));
    }

    // ------------------------------------------------------------------ guards

    [Fact]
    public async Task Handlers_reject_a_null_query()
    {
        var sut = BuildSut();

        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.Dashboard.Handle(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.Alerts.Handle(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.Report.Handle(null!));
    }

    [Fact]
    public async Task MatchRate_CannotExceedOneHundredPercent_WhenHistoryPredatesTheWindow()
    {
        var sut = BuildSut();
        var business = Seed(sut, "Acme");

        // One invoice inside the window, and three confirmed matches whose
        // invoices are far older. Counting matches over all time against a
        // windowed invoice count used to yield 300%.
        var inWindow = SeedInvoice(sut, business.Id);
        sut.Reconciliation.Matches.Add(
            ReconciliationMatch.CreateManual(business.Id, inWindow.Id, Guid.NewGuid(), "u", null));

        for (var i = 0; i < 3; i++)
        {
            var old = Invoice.Create(
                business.Id, Guid.NewGuid().ToString(), InvoiceDirection.Issued, "1.1",
                Today.AddYears(-1), AnotherValidAfm, "ACME AE",
                500m, 0m, 500m, Currency.EUR, null);
            sut.Reconciliation.Invoices.Add(old);
            sut.Reconciliation.Matches.Add(
                ReconciliationMatch.CreateManual(business.Id, old.Id, Guid.NewGuid(), "u", null));
        }

        var result = await sut.Dashboard.Handle(new GetAccountantDashboardQuery());

        var success = result.Should().BeOfType<GetAccountantDashboardResult.Success>().Subject;
        success.Dashboard.Businesses.Should().ContainSingle()
            .Which.MatchRate.Should().BeLessThanOrEqualTo(1m);
    }
}
