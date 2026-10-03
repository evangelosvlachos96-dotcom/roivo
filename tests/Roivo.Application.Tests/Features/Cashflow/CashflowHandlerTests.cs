using FluentAssertions;
using Roivo.Application.Abstractions;
using Roivo.Application.Features.Cashflow.Commands.AddRecurringItem;
using Roivo.Application.Features.Cashflow.Commands.MarkTaxPaid;
using Roivo.Application.Features.Cashflow.Queries.GetCashflowDashboard;
using Roivo.Application.Features.Cashflow.Queries.GetCashflowForecast;
using Roivo.Application.Features.Cashflow.Queries.GetTaxCalendar;
using Roivo.Application.Features.Cashflow.Services;
using Roivo.Application.Tests.Fakes;
using Roivo.Core.Domain.Auditing;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Tests.Features.Cashflow;

/// <summary>Application-layer handler tests for the M7 cashflow feature.</summary>
public class CashflowHandlerTests
{
    private const string ValidAfm = "094014201";

    private static readonly Guid BusinessId = Guid.NewGuid();

    // ---------------------------------------------------------------- helpers

    private static Business SeedBusiness(FakeBusinessRepository repo)
    {
        var business = Business.Create("Acme", ValidAfm, null, null);
        repo.Store[business.Id] = business;
        return business;
    }

    /// <summary>Fills the trailing <paramref name="days"/> with the same daily movement.</summary>
    private static void SeedFlatHistory(
        FakeCashflowRepository repo, int days, decimal inflow = 500m, decimal outflow = 300m)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        for (var i = 1; i <= days; i++)
            repo.Movements.Add(new DailyCashMovement(today.AddDays(-i), inflow, outflow));
    }

    // ------------------------------------------------- MarkTaxPaid (command)

    private sealed record MarkTaxPaidSut(
        MarkTaxPaidHandler Handler,
        FakeCashflowRepository Cashflow,
        FakeAuditWriter Audit,
        FakeTenantContext Tenant);

    private static MarkTaxPaidSut BuildMarkTaxPaidSut()
    {
        var cashflow = new FakeCashflowRepository();
        var audit = new FakeAuditWriter();
        var tenant = new FakeTenantContext();
        return new MarkTaxPaidSut(
            new MarkTaxPaidHandler(cashflow, audit, tenant),
            cashflow, audit, tenant);
    }

    private static TaxObligation SeedObligation(
        FakeCashflowRepository repo, decimal estimated = 4000m, string period = "2026-Q2")
    {
        var obligation = TaxObligation.Create(
            BusinessId,
            TaxType.Vat,
            DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10),
            estimated,
            period);
        repo.TaxObligations.Add(obligation);
        return obligation;
    }

    [Fact]
    public async Task MarkTaxPaid_success_marks_obligation_paid_and_audits()
    {
        var sut = BuildMarkTaxPaidSut();
        var obligation = SeedObligation(sut.Cashflow);
        var paidAt = new DateTime(2026, 7, 20, 9, 30, 0, DateTimeKind.Utc);

        var result = await sut.Handler.Handle(
            new MarkTaxPaidCommand(obligation.Id, 3875.40m, paidAt));

        result.Should().BeOfType<MarkTaxPaidResult.Success>();

        obligation.IsPaid.Should().BeTrue();
        obligation.ActualAmount.Should().Be(3875.40m);
        obligation.PaidAt.Should().Be(paidAt);

        // The forecast reads ExpectedAmount; the actual must now win over the estimate.
        obligation.ExpectedAmount.Should().Be(3875.40m);

        // No duplicate row: the update path must not re-add the tracked entity.
        sut.Cashflow.TaxObligations.Should().ContainSingle();

        var audit = sut.Audit.Calls.Should().ContainSingle().Subject;
        audit.Action.Should().Be(AuditAction.TaxObligationMarkedPaid);
        audit.TenantId.Should().Be(sut.Tenant.CurrentTenantId);
        audit.EntityType.Should().Be(nameof(TaxObligation));
        audit.EntityId.Should().Be(obligation.Id.ToString());
    }

    [Fact]
    public async Task MarkTaxPaid_NotFound_for_unknown_id_and_writes_no_audit()
    {
        var sut = BuildMarkTaxPaidSut();
        SeedObligation(sut.Cashflow);

        var result = await sut.Handler.Handle(
            new MarkTaxPaidCommand(Guid.NewGuid(), 100m, DateTime.UtcNow));

        result.Should().BeOfType<MarkTaxPaidResult.NotFound>();
        sut.Cashflow.TaxObligations.Should().OnlyContain(t => !t.IsPaid);
        sut.Audit.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task MarkTaxPaid_AlreadyPaid_when_obligation_was_already_marked_paid()
    {
        var sut = BuildMarkTaxPaidSut();
        var obligation = SeedObligation(sut.Cashflow);
        obligation.MarkPaid(4000m, new DateTime(2026, 7, 19, 0, 0, 0, DateTimeKind.Utc));

        var result = await sut.Handler.Handle(
            new MarkTaxPaidCommand(obligation.Id, 1m, DateTime.UtcNow));

        result.Should().BeOfType<MarkTaxPaidResult.AlreadyPaid>();

        // The original payment stands — a second attempt must not overwrite it.
        obligation.ActualAmount.Should().Be(4000m);
        sut.Audit.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task MarkTaxPaid_InvalidAmount_for_negative_amount_and_writes_no_audit()
    {
        var sut = BuildMarkTaxPaidSut();
        var obligation = SeedObligation(sut.Cashflow);

        var result = await sut.Handler.Handle(
            new MarkTaxPaidCommand(obligation.Id, -0.01m, DateTime.UtcNow));

        result.Should().BeOfType<MarkTaxPaidResult.InvalidAmount>();
        obligation.IsPaid.Should().BeFalse();
        obligation.ActualAmount.Should().BeNull();
        sut.Audit.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task MarkTaxPaid_accepts_a_zero_payment()
    {
        // A zero-amount ΕΦΚΑ or withholding obligation is normal for a business
        // with no payroll, so zero must not be mistaken for an invalid amount.
        var sut = BuildMarkTaxPaidSut();
        var obligation = SeedObligation(sut.Cashflow, estimated: 0m, period: "2026-06");

        var result = await sut.Handler.Handle(
            new MarkTaxPaidCommand(obligation.Id, 0m, DateTime.UtcNow));

        result.Should().BeOfType<MarkTaxPaidResult.Success>();
        obligation.IsPaid.Should().BeTrue();
        obligation.ActualAmount.Should().Be(0m);
        sut.Audit.Calls.Should().ContainSingle();
    }

    // -------------------------------------------- AddRecurringItem (command)

    private sealed record AddRecurringItemSut(
        AddRecurringItemHandler Handler,
        FakeCashflowRepository Cashflow,
        FakeBusinessRepository Businesses,
        FakeAuditWriter Audit,
        FakeTenantContext Tenant);

    private static AddRecurringItemSut BuildAddRecurringItemSut()
    {
        var cashflow = new FakeCashflowRepository();
        var businesses = new FakeBusinessRepository();
        var audit = new FakeAuditWriter();
        var tenant = new FakeTenantContext();
        return new AddRecurringItemSut(
            new AddRecurringItemHandler(cashflow, businesses, audit, tenant),
            cashflow, businesses, audit, tenant);
    }

    [Fact]
    public async Task AddRecurringItem_success_creates_recurring_category_and_audits()
    {
        var sut = BuildAddRecurringItemSut();
        var business = SeedBusiness(sut.Businesses);

        var result = await sut.Handler.Handle(new AddRecurringItemCommand(
            business.Id, "  Ενοίκιο  ", 1200m, CashflowCategoryType.Expense, RecurringDay: 5));

        var success = result.Should().BeOfType<AddRecurringItemResult.Success>().Subject;

        var category = sut.Cashflow.Categories.Should().ContainSingle().Subject;
        success.CategoryId.Should().Be(category.Id);
        category.BusinessId.Should().Be(business.Id);
        category.Name.Should().Be("Ενοίκιο", "the name is trimmed by the domain");
        category.Type.Should().Be(CashflowCategoryType.Expense);
        category.IsRecurring.Should().BeTrue();
        category.RecurringDay.Should().Be(5);
        category.AverageAmount.Should().Be(1200m);

        var audit = sut.Audit.Calls.Should().ContainSingle().Subject;
        audit.Action.Should().Be(AuditAction.CashflowCategoryCreated);
        audit.TenantId.Should().Be(sut.Tenant.CurrentTenantId);
        audit.EntityType.Should().Be(nameof(CashflowCategory));
        audit.EntityId.Should().Be(category.Id.ToString());
    }

    [Fact]
    public async Task AddRecurringItem_without_a_day_creates_a_non_recurring_category()
    {
        var sut = BuildAddRecurringItemSut();
        var business = SeedBusiness(sut.Businesses);

        var result = await sut.Handler.Handle(new AddRecurringItemCommand(
            business.Id, "Προμήθειες", 80m, CashflowCategoryType.Expense, RecurringDay: null));

        result.Should().BeOfType<AddRecurringItemResult.Success>();

        var category = sut.Cashflow.Categories.Should().ContainSingle().Subject;
        category.IsRecurring.Should().BeFalse();
        category.RecurringDay.Should().BeNull();
    }

    [Fact]
    public async Task AddRecurringItem_NotFound_when_business_does_not_exist()
    {
        var sut = BuildAddRecurringItemSut();

        var result = await sut.Handler.Handle(new AddRecurringItemCommand(
            Guid.NewGuid(), "Ενοίκιο", 1200m, CashflowCategoryType.Expense, RecurringDay: 5));

        result.Should().BeOfType<AddRecurringItemResult.NotFound>();
        sut.Cashflow.Categories.Should().BeEmpty();
        sut.Audit.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task AddRecurringItem_Invalid_when_name_is_blank()
    {
        var sut = BuildAddRecurringItemSut();
        var business = SeedBusiness(sut.Businesses);

        var result = await sut.Handler.Handle(new AddRecurringItemCommand(
            business.Id, "   ", 1200m, CashflowCategoryType.Expense, RecurringDay: 5));

        result.Should().BeOfType<AddRecurringItemResult.Invalid>()
            .Which.Reason.Should().NotBeNullOrWhiteSpace();
        sut.Cashflow.Categories.Should().BeEmpty();
        sut.Audit.Calls.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    [InlineData(-1)]
    public async Task AddRecurringItem_Invalid_when_recurring_day_is_out_of_range(int day)
    {
        var sut = BuildAddRecurringItemSut();
        var business = SeedBusiness(sut.Businesses);

        var result = await sut.Handler.Handle(new AddRecurringItemCommand(
            business.Id, "Ενοίκιο", 1200m, CashflowCategoryType.Expense, RecurringDay: day));

        result.Should().BeOfType<AddRecurringItemResult.Invalid>()
            .Which.Reason.Should().Contain("1");
        sut.Cashflow.Categories.Should().BeEmpty();
        sut.Audit.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task AddRecurringItem_Invalid_when_amount_is_negative()
    {
        var sut = BuildAddRecurringItemSut();
        var business = SeedBusiness(sut.Businesses);

        var result = await sut.Handler.Handle(new AddRecurringItemCommand(
            business.Id, "Ενοίκιο", -1m, CashflowCategoryType.Expense, RecurringDay: 5));

        result.Should().BeOfType<AddRecurringItemResult.Invalid>();
        sut.Cashflow.Categories.Should().BeEmpty();
        sut.Audit.Calls.Should().BeEmpty();
    }

    // ----------------------------------------------- GetTaxCalendar (query)


    /// <summary>
    /// A window that is always in the future, so the handler persists into it.
    /// Absolute dates rot: once they fall into the past the handler stops
    /// writing, by design.
    /// </summary>
    private static (DateOnly From, DateOnly To) FutureWindow(int months = 6)
    {
        var from = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        return (from, from.AddMonths(months));
    }

    private static (GetTaxCalendarHandler Handler, FakeCashflowRepository Repo) BuildTaxCalendarSut()
    {
        var repo = new FakeCashflowRepository();
        return (new GetTaxCalendarHandler(repo, new GreekTaxCalendar(repo)), repo);
    }

    [Fact]
    public async Task GetTaxCalendar_generates_and_persists_obligations_on_first_call()
    {
        var (handler, repo) = BuildTaxCalendarSut();
        var (from, to) = FutureWindow();

        var calendar = await handler.Handle(new GetTaxCalendarQuery(BusinessId, from, to));

        calendar.From.Should().Be(from);
        calendar.To.Should().Be(to);
        calendar.Obligations.Should().NotBeEmpty();
        calendar.Obligations.Should().BeInAscendingOrder(o => o.DueDate);
        calendar.Obligations.Should().OnlyContain(o => o.DueDate >= from && o.DueDate <= to);

        // They are persisted, not just computed.
        repo.TaxObligations.Should().HaveCount(calendar.Obligations.Count);
    }

    [Fact]
    public async Task GetTaxCalendar_second_call_over_the_same_window_does_not_duplicate()
    {
        var (handler, repo) = BuildTaxCalendarSut();
        var (windowFrom, windowTo) = FutureWindow(12);
        var query = new GetTaxCalendarQuery(BusinessId, windowFrom, windowTo);

        var first = await handler.Handle(query);
        var storedAfterFirst = repo.TaxObligations.Select(o => o.Id).ToList();

        var second = await handler.Handle(query);

        second.Obligations.Should().HaveCount(first.Obligations.Count);
        repo.TaxObligations.Should().HaveCount(storedAfterFirst.Count);

        // The rows survive regeneration with their identities intact, which is
        // what keeps a recorded payment attached to its obligation.
        repo.TaxObligations.Select(o => o.Id).Should().BeEquivalentTo(storedAfterFirst);

        // The upsert key (business, type, period) really is unique.
        repo.TaxObligations
            .GroupBy(o => (o.BusinessId, o.TaxType, o.Period))
            .Should().OnlyContain(g => g.Count() == 1);
    }

    [Fact]
    public async Task GetTaxCalendar_keeps_a_recorded_payment_across_a_regeneration()
    {
        var (handler, repo) = BuildTaxCalendarSut();
        var (paymentFrom, paymentTo) = FutureWindow();
        var query = new GetTaxCalendarQuery(BusinessId, paymentFrom, paymentTo);

        var first = await handler.Handle(query);
        var paid = first.Obligations[0];
        paid.MarkPaid(123.45m, DateTime.UtcNow);

        var second = await handler.Handle(query);

        second.Obligations.Should().Contain(o => o.Id == paid.Id && o.IsPaid && o.ActualAmount == 123.45m);
    }

    [Fact]
    public async Task GetTaxCalendar_inverted_range_returns_empty_and_persists_nothing()
    {
        var (handler, repo) = BuildTaxCalendarSut();
        var from = new DateOnly(2026, 6, 30);
        var to = new DateOnly(2026, 1, 1);

        var calendar = await handler.Handle(new GetTaxCalendarQuery(BusinessId, from, to));

        calendar.From.Should().Be(from);
        calendar.To.Should().Be(to);
        calendar.Obligations.Should().BeEmpty();
        repo.TaxObligations.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTaxCalendar_returns_obligations_scoped_to_the_business()
    {
        var (handler, repo) = BuildTaxCalendarSut();
        var otherBusiness = Guid.NewGuid();
        repo.TaxObligations.Add(TaxObligation.Create(
            otherBusiness, TaxType.Vat, new DateOnly(2026, 4, 20), 500m, "2026-Q1"));

        var calendar = await handler.Handle(new GetTaxCalendarQuery(
            BusinessId, new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30)));

        calendar.Obligations.Should().OnlyContain(o => o.BusinessId == BusinessId);
    }

    // ------------------------------------------ GetCashflowDashboard (query)

    private static (GetCashflowDashboardHandler Handler, FakeCashflowRepository Repo) BuildDashboardSut()
    {
        var repo = new FakeCashflowRepository();
        var engine = new CashflowForecastEngine(repo, new GreekTaxCalendar(repo));
        return (new GetCashflowDashboardHandler(engine, repo), repo);
    }

    [Fact]
    public async Task GetCashflowDashboard_HasSufficientHistory_false_with_no_history()
    {
        var (handler, repo) = BuildDashboardSut();
        repo.CurrentBalance = 7500m;

        var dashboard = await handler.Handle(new GetCashflowDashboardQuery(BusinessId));

        dashboard.BusinessId.Should().Be(BusinessId);
        dashboard.HasSufficientHistory.Should().BeFalse();
        dashboard.DailyForecasts.Should().BeEmpty();

        // The balance is still reported even when the projection is withheld.
        dashboard.Summary.CurrentBalance.Should().Be(7500m);
    }

    [Fact]
    public async Task GetCashflowDashboard_HasSufficientHistory_false_with_too_little_history()
    {
        var (handler, repo) = BuildDashboardSut();
        repo.CurrentBalance = 4200m;
        SeedFlatHistory(repo, days: CashflowForecastEngine.MinimumHistoryDays - 1);

        var dashboard = await handler.Handle(new GetCashflowDashboardQuery(BusinessId, DaysAhead: 30));

        dashboard.HasSufficientHistory.Should().BeFalse();
        dashboard.Summary.CurrentBalance.Should().Be(4200m);
    }

    [Fact]
    public async Task GetCashflowDashboard_HasSufficientHistory_true_at_the_minimum_history()
    {
        var (handler, repo) = BuildDashboardSut();
        repo.CurrentBalance = 10_000m;
        SeedFlatHistory(repo, days: CashflowForecastEngine.MinimumHistoryDays);

        var dashboard = await handler.Handle(new GetCashflowDashboardQuery(BusinessId, DaysAhead: 30));

        dashboard.HasSufficientHistory.Should().BeTrue();
        dashboard.Summary.CurrentBalance.Should().Be(10_000m);
    }

    [Fact]
    public async Task GetCashflowDashboard_carries_the_engine_projection_through()
    {
        var (handler, repo) = BuildDashboardSut();
        repo.CurrentBalance = 20_000m;
        SeedFlatHistory(repo, days: 120, inflow: 500m, outflow: 300m);

        var dueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(15);
        repo.TaxObligations.Add(TaxObligation.Create(
            BusinessId, TaxType.Vat, dueDate, 4000m, "2026-Q2"));

        var dashboard = await handler.Handle(new GetCashflowDashboardQuery(BusinessId, DaysAhead: 45));

        dashboard.HasSufficientHistory.Should().BeTrue();
        dashboard.DailyForecasts.Should().HaveCount(45);
        dashboard.Summary.CurrentBalance.Should().Be(20_000m);
        dashboard.Summary.AverageDailyInflow.Should().Be(500m);
        dashboard.UpcomingTaxObligations.Should().ContainSingle();
        dashboard.Alerts.Should().Contain(a => a.Type == CashflowAlertType.TaxDue);
    }

    // ------------------------------------------- GetCashflowForecast (query)

    /// <summary>Records the arguments it was asked for and hands back a known result.</summary>
    private sealed class CapturingForecastEngine : ICashflowForecastEngine
    {
        public List<(Guid BusinessId, int DaysAhead)> Calls { get; } = [];

        public CashflowForecastResult NextResult { get; set; } = CashflowForecastResult.Empty;

        public Task<CashflowForecastResult> ForecastAsync(
            Guid businessId, int daysAhead = 90, CancellationToken cancellationToken = default)
        {
            Calls.Add((businessId, daysAhead));
            return Task.FromResult(NextResult);
        }
    }

    [Fact]
    public async Task GetCashflowForecast_returns_the_engine_result_and_forwards_the_horizon()
    {
        var engine = new CapturingForecastEngine
        {
            NextResult = CashflowForecastResult.Empty with
            {
                Summary = new CashflowSummary(
                    1234m, 1m, 2m, 3m, 4m, 5m, 6m, CashflowSummary.UnlimitedRunway),
            },
        };
        var handler = new GetCashflowForecastHandler(engine);

        var result = await handler.Handle(new GetCashflowForecastQuery(BusinessId, DaysAhead: 45));

        result.Should().BeSameAs(engine.NextResult);
        engine.Calls.Should().ContainSingle().Which.Should().Be((BusinessId, 45));
    }

    [Fact]
    public async Task GetCashflowForecast_defaults_to_a_ninety_day_horizon()
    {
        var engine = new CapturingForecastEngine();
        var handler = new GetCashflowForecastHandler(engine);

        await handler.Handle(new GetCashflowForecastQuery(BusinessId));

        engine.Calls.Should().ContainSingle().Which.DaysAhead.Should().Be(90);
    }

    [Fact]
    public async Task GetCashflowForecast_over_the_real_engine_projects_the_requested_days()
    {
        var repo = new FakeCashflowRepository { CurrentBalance = 5000m };
        var handler = new GetCashflowForecastHandler(
            new CashflowForecastEngine(repo, new GreekTaxCalendar(repo)));
        SeedFlatHistory(repo, days: 90, inflow: 200m, outflow: 100m);

        var result = await handler.Handle(new GetCashflowForecastQuery(BusinessId, DaysAhead: 30));

        result.DailyForecasts.Should().HaveCount(30);
        result.Summary.CurrentBalance.Should().Be(5000m);
    }

    [Fact]
    public async Task GetTaxCalendar_ViewingAPastQuarter_DoesNotManufactureOverdueObligations()
    {
        var (handler, repo) = BuildTaxCalendarSut();

        // A user paging back through history must not create unpaid rows for
        // periods that are already over — nobody would ever mark them paid, so
        // they would show as permanently overdue.
        var lastYear = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-1);
        var from = new DateOnly(lastYear.Year, 1, 1);
        var to = new DateOnly(lastYear.Year, 3, 31);

        var result = await handler.Handle(new GetTaxCalendarQuery(Guid.NewGuid(), from, to));

        result.Obligations.Should().BeEmpty();
        repo.TaxObligations.Should().BeEmpty("a read must not write history");
    }

    [Fact]
    public async Task GetTaxCalendar_AFutureWindowStillPersists()
    {
        var (handler, repo) = BuildTaxCalendarSut();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await handler.Handle(
            new GetTaxCalendarQuery(Guid.NewGuid(), today, today.AddDays(120)));

        result.Obligations.Should().NotBeEmpty();
        repo.TaxObligations.Should().OnlyContain(o => o.DueDate >= today);
    }
}
