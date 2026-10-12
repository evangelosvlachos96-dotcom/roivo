using FluentAssertions;
using Roivo.Application.Abstractions;
using Roivo.Application.Features.Cashflow.Services;
using Roivo.Application.Tests.Fakes;
using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Tests.Features.Cashflow;

/// <summary>
/// The parts of the calendar that depend on what the business itself has told
/// Roivo: how often it files VAT, and whether it owns property.
/// </summary>
public class GreekTaxCalendarBusinessProfileTests
{
    private static readonly Guid BusinessId = Guid.NewGuid();

    // Dates are anchored to a frozen clock rather than written as literals, so
    // the expectations stay true whatever year the suite runs in.
    private static readonly FakeClock Clock = new(new DateOnly(2026, 6, 15));

    private static int Year => Clock.Today.Year;

    private static (GreekTaxCalendar Calendar, FakeCashflowRepository Repo) BuildSut(
        VatFrequency frequency = VatFrequency.Quarterly,
        decimal? propertyValue = null)
    {
        var repo = new FakeCashflowRepository
        {
            TaxProfile = new BusinessTaxProfile(frequency, propertyValue),
        };

        return (new GreekTaxCalendar(repo), repo);
    }

    // ---- ΕΝΦΙΑ ------------------------------------------------------------

    [Fact]
    public async Task PropertyTax_FiveInstalmentsFromSeptemberThroughJanuary()
    {
        var (calendar, _) = BuildSut(propertyValue: 300_000m);

        var obligations = await calendar.GenerateAsync(
            BusinessId, new DateOnly(Year, 9, 1), new DateOnly(Year + 1, 1, 31));

        var enfia = obligations.Where(o => o.TaxType == TaxType.PropertyTax).ToList();

        enfia.Should().HaveCount(GreekTaxCalendar.PropertyTaxInstalments);
        enfia.Select(o => (o.DueDate, o.Period)).Should().Equal(
            (new DateOnly(Year, 9, 30), $"{Year}-E1"),
            (new DateOnly(Year, 10, 31), $"{Year}-E2"),
            (new DateOnly(Year, 11, 30), $"{Year}-E3"),
            (new DateOnly(Year, 12, 31), $"{Year}-E4"),
            (new DateOnly(Year + 1, 1, 31), $"{Year}-E5"));
    }

    [Fact]
    public async Task PropertyTax_NotGeneratedAtAllWhenValueIsUnknown()
    {
        var (calendar, _) = BuildSut(propertyValue: null);

        // The whole window the instalments could fall in, so a missing
        // obligation cannot be an artefact of the range.
        var obligations = await calendar.GenerateAsync(
            BusinessId, new DateOnly(Year, 1, 1), new DateOnly(Year + 1, 12, 31));

        obligations.Should().NotContain(o => o.TaxType == TaxType.PropertyTax);
    }

    [Fact]
    public async Task PropertyTax_MissingBusinessBehavesLikeUnknownValue()
    {
        var (calendar, repo) = BuildSut();
        repo.TaxProfile = null;

        var obligations = await calendar.GenerateAsync(
            BusinessId, new DateOnly(Year, 9, 1), new DateOnly(Year + 1, 1, 31));

        obligations.Should().NotContain(o => o.TaxType == TaxType.PropertyTax);
    }

    [Fact]
    public async Task PropertyTax_InstalmentIsValueTimesRateSpreadOverFive()
    {
        const decimal propertyValue = 300_000m;
        var (calendar, _) = BuildSut(propertyValue: propertyValue);

        var obligations = await calendar.GenerateAsync(
            BusinessId, new DateOnly(Year, 9, 1), new DateOnly(Year + 1, 1, 31));

        var expected = Math.Round(
            propertyValue * GreekTaxCalendar.CoarsePropertyTaxProjectionRate
                / GreekTaxCalendar.PropertyTaxInstalments,
            2,
            MidpointRounding.AwayFromZero);

        expected.Should().Be(300m);
        obligations.Where(o => o.TaxType == TaxType.PropertyTax)
            .Should().OnlyContain(o => o.EstimatedAmount == expected);
    }

    [Fact]
    public async Task PropertyTax_ZeroValueProducesZeroInstalments()
    {
        var (calendar, _) = BuildSut(propertyValue: 0m);

        var obligations = await calendar.GenerateAsync(
            BusinessId, new DateOnly(Year, 9, 1), new DateOnly(Year + 1, 1, 31));

        // Zero is a stated value, not an unknown one: the instalments exist and
        // happen to cost nothing, which is different from having no property.
        var enfia = obligations.Where(o => o.TaxType == TaxType.PropertyTax).ToList();
        enfia.Should().HaveCount(GreekTaxCalendar.PropertyTaxInstalments);
        enfia.Should().OnlyContain(o => o.EstimatedAmount == 0m);
    }

    [Fact]
    public void PropertyTax_DueDatesInOmitsInstalmentsByDefault()
    {
        var (calendar, _) = BuildSut();

        calendar.DueDatesIn(new DateOnly(Year, 9, 1), new DateOnly(Year + 1, 1, 31))
            .Should().NotContain(d => d.TaxType == TaxType.PropertyTax);

        calendar.DueDatesIn(
                new DateOnly(Year, 9, 1), new DateOnly(Year + 1, 1, 31),
                includePropertyTax: true)
            .Count(d => d.TaxType == TaxType.PropertyTax)
            .Should().Be(GreekTaxCalendar.PropertyTaxInstalments);
    }

    // ---- Monthly versus quarterly ΦΠΑ -------------------------------------

    [Fact]
    public async Task MonthlyVat_TwelveFilingsAYearOnTheTwentieth()
    {
        var (calendar, _) = BuildSut(VatFrequency.Monthly);

        var obligations = await calendar.GenerateAsync(
            BusinessId, new DateOnly(Year, 1, 1), new DateOnly(Year, 12, 31));

        var vat = obligations.Where(o => o.TaxType == TaxType.Vat).ToList();

        vat.Should().HaveCount(12);
        vat.Should().OnlyContain(o => o.DueDate.Day == 20);

        // January's filing settles the previous December.
        vat[0].Period.Should().Be($"{Year - 1}-12");
        vat[0].DueDate.Should().Be(new DateOnly(Year, 1, 20));

        vat[11].Period.Should().Be($"{Year}-11");
        vat.Select(o => o.Period).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task QuarterlyVat_StillFourFilingsAYear()
    {
        var (calendar, _) = BuildSut(VatFrequency.Quarterly);

        var obligations = await calendar.GenerateAsync(
            BusinessId, new DateOnly(Year, 1, 1), new DateOnly(Year, 12, 31));

        var vat = obligations.Where(o => o.TaxType == TaxType.Vat).ToList();

        vat.Should().HaveCount(4);
        vat.Select(o => o.Period).Should().Equal(
            $"{Year - 1}-Q4", $"{Year}-Q1", $"{Year}-Q2", $"{Year}-Q3");
    }

    [Theory]
    [InlineData(VatFrequency.Monthly)]
    [InlineData(VatFrequency.Quarterly)]
    public async Task Vat_IsNeverBothMonthlyAndQuarterly(VatFrequency frequency)
    {
        var (calendar, _) = BuildSut(frequency);

        var obligations = await calendar.GenerateAsync(
            BusinessId, new DateOnly(Year, 1, 1), new DateOnly(Year, 12, 31));

        var periods = obligations
            .Where(o => o.TaxType == TaxType.Vat)
            .Select(o => o.Period)
            .ToList();

        var quarterly = periods.Where(p => p.Contains('Q', StringComparison.Ordinal)).ToList();
        var monthly = periods.Except(quarterly).ToList();

        if (frequency == VatFrequency.Monthly)
        {
            quarterly.Should().BeEmpty();
            monthly.Should().HaveCount(12);
        }
        else
        {
            monthly.Should().BeEmpty();
            quarterly.Should().HaveCount(4);
        }

        // One filing per month at most, regardless of frequency: a business
        // that paid both would be double-charged in the forecast.
        obligations
            .Where(o => o.TaxType == TaxType.Vat)
            .Should().OnlyHaveUniqueItems()
            .And.Subject.Select(o => o.DueDate).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task MonthlyVat_EstimateReadsTheMonthNotTheQuarter()
    {
        var (calendar, repo) = BuildSut(VatFrequency.Monthly);
        repo.VatTotals = new VatTotals(OutputVat: 2000m, InputVat: 500m);

        var obligations = await calendar.GenerateAsync(
            BusinessId, new DateOnly(Year, 4, 1), new DateOnly(Year, 4, 30));

        obligations.Single(o => o.TaxType == TaxType.Vat).EstimatedAmount.Should().Be(1500m);

        // April's monthly filing settles March, so the totals query must cover
        // March alone — a quarter range here would triple the estimate.
        repo.VatTotalsRequests.Should().ContainSingle()
            .Which.Should().Be((new DateOnly(Year, 3, 1), new DateOnly(Year, 3, 31)));
    }

    [Fact]
    public async Task QuarterlyVat_EstimateReadsTheWholeQuarter()
    {
        var (calendar, repo) = BuildSut(VatFrequency.Quarterly);

        await calendar.GenerateAsync(BusinessId, new DateOnly(Year, 4, 1), new DateOnly(Year, 4, 30));

        repo.VatTotalsRequests.Should().ContainSingle()
            .Which.Should().Be((new DateOnly(Year, 1, 1), new DateOnly(Year, 3, 31)));
    }

    [Theory]
    [InlineData("2026-01", "2026-01-01", "2026-01-31")]
    [InlineData("2026-02", "2026-02-01", "2026-02-28")]
    [InlineData("2026-12", "2026-12-01", "2026-12-31")]
    public void MonthRange_MapsPeriodToCalendarMonth(string period, string expectedFrom, string expectedTo)
    {
        var (from, to) = GreekTaxCalendar.VatPeriodRange(period);

        from.Should().Be(DateOnly.Parse(expectedFrom));
        to.Should().Be(DateOnly.Parse(expectedTo));
    }

    [Fact]
    public void VatPeriodRange_StillUnderstandsQuarters()
    {
        GreekTaxCalendar.VatPeriodRange("2026-Q3")
            .Should().Be((new DateOnly(2026, 7, 1), new DateOnly(2026, 9, 30)));
    }
}
