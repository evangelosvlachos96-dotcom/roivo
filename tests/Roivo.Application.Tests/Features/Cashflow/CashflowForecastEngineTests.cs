using FluentAssertions;
using Roivo.Application.Abstractions;
using Roivo.Application.Features.Cashflow.Services;
using Roivo.Application.Tests.Fakes;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Tests.Features.Cashflow;

public class CashflowForecastEngineTests
{
    private static readonly Guid BusinessId = Guid.NewGuid();

    private static (CashflowForecastEngine Engine, FakeCashflowRepository Repo) BuildSut()
    {
        var repo = new FakeCashflowRepository();
        return (new CashflowForecastEngine(repo, new GreekTaxCalendar(repo)), repo);
    }

    /// <summary>Fills the trailing <paramref name="days"/> with the same daily movement.</summary>
    private static void SeedFlatHistory(
        FakeCashflowRepository repo, int days, decimal inflow, decimal outflow)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        for (var i = 1; i <= days; i++)
            repo.Movements.Add(new DailyCashMovement(today.AddDays(-i), inflow, outflow));
    }

    [Fact]
    public async Task NoData_ReturnsZeroForecast()
    {
        var (engine, repo) = BuildSut();
        repo.CurrentBalance = 5000m;

        var result = await engine.ForecastAsync(BusinessId);

        result.DailyForecasts.Should().BeEmpty();
        result.Alerts.Should().BeEmpty();

        // The balance is still reported; only the projection is withheld.
        result.Summary.CurrentBalance.Should().Be(5000m);
        result.Summary.Forecast90DayBalance.Should().Be(5000m);
        result.Summary.AverageDailyInflow.Should().Be(0m);
        result.Summary.DaysOfRunway.Should().Be(CashflowSummary.UnlimitedRunway);
    }

    [Fact]
    public async Task RegularIncome_PredictsFutureIncome()
    {
        var (engine, repo) = BuildSut();
        repo.CurrentBalance = 10_000m;
        SeedFlatHistory(repo, days: 120, inflow: 500m, outflow: 300m);

        var result = await engine.ForecastAsync(BusinessId, daysAhead: 30);

        result.DailyForecasts.Should().HaveCount(30);
        result.DailyForecasts.Should().OnlyContain(d => d.PredictedInflow == 500m);
        result.DailyForecasts.Should().OnlyContain(d => d.PredictedOutflow == 300m);

        // Net +200/day on a 10,000 opening balance.
        result.DailyForecasts[0].PredictedBalance.Should().Be(10_200m);
        result.DailyForecasts[29].PredictedBalance.Should().Be(16_000m);

        result.Summary.AverageDailyInflow.Should().Be(500m);
        result.Summary.BurnRate.Should().Be(0m, "the business is cash-positive");
        result.Summary.DaysOfRunway.Should().Be(CashflowSummary.UnlimitedRunway);
    }

    [Fact]
    public async Task RecurringExpense_AppearsInForecast()
    {
        var (engine, repo) = BuildSut();
        repo.CurrentBalance = 50_000m;
        SeedFlatHistory(repo, days: 120, inflow: 100m, outflow: 100m);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var rentDay = today.AddDays(10).Day;

        repo.Categories.Add(CashflowCategory.Create(
            BusinessId, "Ενοίκιο", CashflowCategoryType.Expense,
            isRecurring: true, recurringDay: rentDay, averageAmount: 1200m));

        var result = await engine.ForecastAsync(BusinessId, daysAhead: 60);

        var rentDays = result.DailyForecasts.Where(d => d.Date.Day == rentDay).ToList();
        rentDays.Should().NotBeEmpty();
        rentDays.Should().OnlyContain(d => d.PredictedOutflow == 1300m);

        // Every other day keeps the baseline.
        result.DailyForecasts.Where(d => d.Date.Day != rentDay)
            .Should().OnlyContain(d => d.PredictedOutflow == 100m);
    }

    [Fact]
    public async Task TaxObligation_IncludedInOutflow()
    {
        var (engine, repo) = BuildSut();
        repo.CurrentBalance = 20_000m;
        SeedFlatHistory(repo, days: 120, inflow: 200m, outflow: 100m);

        var dueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(15);
        repo.TaxObligations.Add(TaxObligation.Create(
            BusinessId, TaxType.Vat, dueDate, estimatedAmount: 4000m, period: "2026-Q2"));

        var result = await engine.ForecastAsync(BusinessId, daysAhead: 30);

        var dueDay = result.DailyForecasts.Single(d => d.Date == dueDate);
        dueDay.PredictedOutflow.Should().Be(4100m);

        result.UpcomingTaxObligations.Should().ContainSingle();
        result.Alerts.Should().Contain(a => a.Type == CashflowAlertType.TaxDue && a.Date == dueDate);
    }

    [Fact]
    public async Task PaidTaxObligation_IsNotCharged()
    {
        var (engine, repo) = BuildSut();
        repo.CurrentBalance = 20_000m;
        SeedFlatHistory(repo, days: 120, inflow: 200m, outflow: 100m);

        var dueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10);
        var obligation = TaxObligation.Create(
            BusinessId, TaxType.Vat, dueDate, estimatedAmount: 4000m, period: "2026-Q2");
        obligation.MarkPaid(3900m, DateTime.UtcNow);
        repo.TaxObligations.Add(obligation);

        var result = await engine.ForecastAsync(BusinessId, daysAhead: 30);

        result.DailyForecasts.Single(d => d.Date == dueDate).PredictedOutflow.Should().Be(100m);
        result.UpcomingTaxObligations.Should().BeEmpty();
    }

    [Fact]
    public async Task NegativeBalance_GeneratesAlert()
    {
        var (engine, repo) = BuildSut();
        repo.CurrentBalance = 1000m;
        SeedFlatHistory(repo, days: 120, inflow: 0m, outflow: 100m);

        var result = await engine.ForecastAsync(BusinessId, daysAhead: 30);

        var alert = result.Alerts.Should().ContainSingle(a => a.Type == CashflowAlertType.NegativeBalance).Subject;
        alert.Severity.Should().Be(CashflowAlertSeverity.Critical);

        // 1000 at -100/day crosses zero after day 10.
        result.Summary.DaysOfRunway.Should().Be(10);
        result.Summary.BurnRate.Should().Be(100m);
    }

    [Fact]
    public async Task LowBalance_GeneratesWarningBeforeGoingNegative()
    {
        var (engine, repo) = BuildSut();
        repo.CurrentBalance = 1500m;
        SeedFlatHistory(repo, days: 120, inflow: 0m, outflow: 100m);

        var result = await engine.ForecastAsync(BusinessId, daysAhead: 30);

        var low = result.Alerts.Should().ContainSingle(a => a.Type == CashflowAlertType.LowBalance).Subject;
        low.Severity.Should().Be(CashflowAlertSeverity.Warning);
        low.Amount.Should().BeLessThan(CashflowForecastEngine.LowBalanceThreshold);
    }

    [Fact]
    public async Task ConfidenceInterval_WidensOverTime()
    {
        var (engine, repo) = BuildSut();
        repo.CurrentBalance = 10_000m;
        SeedFlatHistory(repo, days: 120, inflow: 500m, outflow: 300m);

        var result = await engine.ForecastAsync(BusinessId, daysAhead: 90);

        var spreads = result.DailyForecasts
            .Select(d => d.ConfidenceHigh - d.ConfidenceLow)
            .ToList();

        spreads[0].Should().BeGreaterThan(0m);
        spreads.Should().BeInAscendingOrder();
        spreads[^1].Should().BeGreaterThan(spreads[0]);

        result.DailyForecasts.Should().OnlyContain(d =>
            d.ConfidenceLow <= d.PredictedBalance && d.PredictedBalance <= d.ConfidenceHigh);
    }

    [Fact]
    public async Task ZeroDaysAhead_ReturnsEmpty()
    {
        var (engine, repo) = BuildSut();
        SeedFlatHistory(repo, days: 60, inflow: 100m, outflow: 50m);

        var result = await engine.ForecastAsync(BusinessId, daysAhead: 0);

        result.DailyForecasts.Should().BeEmpty();
    }

    [Fact]
    public async Task ShortHorizon_ReportsItsOwnLastDayForLongerMilestones()
    {
        var (engine, repo) = BuildSut();
        repo.CurrentBalance = 1000m;
        SeedFlatHistory(repo, days: 60, inflow: 100m, outflow: 0m);

        var result = await engine.ForecastAsync(BusinessId, daysAhead: 10);

        result.DailyForecasts.Should().HaveCount(10);

        // With only 10 days projected, the 30/60/90 milestones all fall back to day 10.
        var lastBalance = result.DailyForecasts[^1].PredictedBalance;
        result.Summary.Forecast30DayBalance.Should().Be(lastBalance);
        result.Summary.Forecast90DayBalance.Should().Be(lastBalance);
    }
}
