using FluentAssertions;
using Roivo.Application.Features.Accountant.Services;
using Roivo.Application.Features.Cashflow.Services;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Tests.Features.Accountant;

/// <summary>
/// Boundary tests for the traffic-light classification. The thresholds are the
/// one piece of judgement in the accountant workspace, so they are pinned at
/// the exact day either side of each edge.
/// </summary>
public class CashflowHealthTests
{
    private static readonly DateOnly Today = new(2026, 6, 15);

    [Theory]
    [InlineData(0, CashflowHealth.Red)]
    [InlineData(29, CashflowHealth.Red)]
    [InlineData(30, CashflowHealth.Yellow)]
    [InlineData(89, CashflowHealth.Yellow)]
    [InlineData(90, CashflowHealth.Green)]
    [InlineData(365, CashflowHealth.Green)]
    [InlineData(CashflowSummary.UnlimitedRunway, CashflowHealth.Green)]
    public void FromDaysOfRunway_classifies_at_the_boundaries(int days, string expected)
        => CashflowHealth.FromDaysOfRunway(days).Should().Be(expected);

    [Fact]
    public void FromDaysOfRunway_maps_no_forecast_to_unknown_not_green()
        => CashflowHealth.FromDaysOfRunway(null).Should().Be(CashflowHealth.Unknown);

    [Theory]
    [InlineData(CashflowHealth.Red, true)]
    [InlineData(CashflowHealth.Yellow, true)]
    [InlineData(CashflowHealth.Green, false)]
    [InlineData(CashflowHealth.Unknown, false)]
    public void IsWarning_covers_only_the_amber_and_red_colours(string health, bool expected)
        => CashflowHealth.IsWarning(health).Should().Be(expected);

    [Fact]
    public void RunwayFromStoredForecasts_returns_null_when_nothing_is_stored()
        => CashflowHealth.RunwayFromStoredForecasts([], Today).Should().BeNull();

    [Fact]
    public void RunwayFromStoredForecasts_ignores_days_already_past()
    {
        var forecasts = new[] { Forecast(Today.AddDays(-1), -500m) };

        CashflowHealth.RunwayFromStoredForecasts(forecasts, Today).Should().BeNull();
    }

    [Fact]
    public void RunwayFromStoredForecasts_is_unlimited_when_the_projection_never_dips()
    {
        var forecasts = Enumerable.Range(1, 90).Select(d => Forecast(Today.AddDays(d), 1_000m));

        CashflowHealth.RunwayFromStoredForecasts(forecasts, Today)
            .Should().Be(CashflowSummary.UnlimitedRunway);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(30, 29)]
    [InlineData(31, 30)]
    [InlineData(90, 89)]
    [InlineData(91, 90)]
    public void RunwayFromStoredForecasts_counts_whole_days_before_the_first_shortfall(
        int negativeOnDay, int expectedRunway)
    {
        var forecasts = Enumerable.Range(1, 120)
            .Select(d => Forecast(Today.AddDays(d), d >= negativeOnDay ? -1m : 1_000m));

        CashflowHealth.RunwayFromStoredForecasts(forecasts, Today).Should().Be(expectedRunway);
    }

    [Theory]
    [InlineData(30, CashflowHealth.Red)]
    [InlineData(31, CashflowHealth.Yellow)]
    [InlineData(90, CashflowHealth.Yellow)]
    [InlineData(91, CashflowHealth.Green)]
    public void Stored_forecasts_classify_consistently_with_the_thresholds(
        int negativeOnDay, string expected)
    {
        var forecasts = Enumerable.Range(1, 120)
            .Select(d => Forecast(Today.AddDays(d), d >= negativeOnDay ? -1m : 1_000m));

        var runway = CashflowHealth.RunwayFromStoredForecasts(forecasts, Today);

        CashflowHealth.FromDaysOfRunway(runway).Should().Be(expected);
    }

    [Fact]
    public void RunwayFromStoredForecasts_takes_the_earliest_shortfall_whatever_the_order()
    {
        var forecasts = new[]
        {
            Forecast(Today.AddDays(40), -100m),
            Forecast(Today.AddDays(11), -100m),
            Forecast(Today.AddDays(70), -100m),
        };

        CashflowHealth.RunwayFromStoredForecasts(forecasts, Today).Should().Be(10);
    }

    private static CashflowForecast Forecast(DateOnly date, decimal balance)
        => CashflowForecast.Create(
            Guid.NewGuid(),
            date,
            predictedInflow: 0m,
            predictedOutflow: 0m,
            predictedBalance: balance,
            confidenceLow: balance - 1m,
            confidenceHigh: balance + 1m);
}
