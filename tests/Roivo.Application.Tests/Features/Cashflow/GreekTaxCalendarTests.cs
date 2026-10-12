using FluentAssertions;
using Roivo.Application.Abstractions;
using Roivo.Application.Features.Cashflow.Services;
using Roivo.Application.Tests.Fakes;
using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Tests.Features.Cashflow;

public class GreekTaxCalendarTests
{
    private static readonly Guid BusinessId = Guid.NewGuid();

    private static (GreekTaxCalendar Calendar, FakeCashflowRepository Repo) BuildSut()
    {
        var repo = new FakeCashflowRepository();
        return (new GreekTaxCalendar(repo), repo);
    }

    [Fact]
    public void VATQuarterly_CorrectDueDates()
    {
        var (calendar, _) = BuildSut();

        var dates = calendar.DueDatesIn(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31))
            .Where(d => d.TaxType == TaxType.Vat)
            .ToList();

        dates.Should().HaveCount(4);

        // January settles the prior year's Q4; the rest settle the quarter just closed.
        dates.Select(d => (d.DueDate, d.Period)).Should().Equal(
            (new DateOnly(2026, 1, 20), "2025-Q4"),
            (new DateOnly(2026, 4, 20), "2026-Q1"),
            (new DateOnly(2026, 7, 20), "2026-Q2"),
            (new DateOnly(2026, 10, 20), "2026-Q3"));
    }

    [Fact]
    public void IncomeTaxInstalments_CorrectDates()
    {
        var (calendar, _) = BuildSut();

        var dates = calendar.DueDatesIn(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31))
            .Where(d => d.TaxType == TaxType.IncomeTax)
            .ToList();

        dates.Should().HaveCount(3);
        dates.Select(d => d.DueDate).Should().Equal(
            new DateOnly(2026, 7, 31),
            new DateOnly(2026, 9, 30),
            new DateOnly(2026, 11, 30));

        // All three settle the prior tax year.
        dates.Should().OnlyContain(d => d.Period.StartsWith("2025"));
    }

    [Fact]
    public void SocialSecurity_MonthlyDueDates()
    {
        var (calendar, _) = BuildSut();

        var dates = calendar.DueDatesIn(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31))
            .Where(d => d.TaxType == TaxType.SocialSecurity)
            .ToList();

        dates.Should().HaveCount(12);
        dates.Should().OnlyContain(d => d.DueDate.Day == DateTime.DaysInMonth(d.DueDate.Year, d.DueDate.Month));

        // February lands on the 28th in 2026, which is the point of the clamp.
        dates.Should().Contain(d => d.DueDate == new DateOnly(2026, 2, 28));
    }

    [Fact]
    public void WithholdingTax_IsMonthlyOnTheTwentieth()
    {
        var (calendar, _) = BuildSut();

        var dates = calendar.DueDatesIn(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31))
            .Where(d => d.TaxType == TaxType.WithholdingTax)
            .ToList();

        dates.Should().HaveCount(12);
        dates.Should().OnlyContain(d => d.DueDate.Day == 20);

        // January's filing settles the previous December.
        dates.First().Period.Should().Be("2025-12");
    }

    [Fact]
    public void ProfessionalTax_IsAnnualWithTheFirstInstalment()
    {
        var (calendar, _) = BuildSut();

        var dates = calendar.DueDatesIn(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31))
            .Where(d => d.TaxType == TaxType.ProfessionalTax)
            .ToList();

        dates.Should().ContainSingle();
        dates[0].DueDate.Should().Be(new DateOnly(2026, 7, 31));
    }

    [Fact]
    public void DueDatesIn_RespectsWindowBoundaries()
    {
        var (calendar, _) = BuildSut();

        var dates = calendar.DueDatesIn(new DateOnly(2026, 4, 21), new DateOnly(2026, 6, 30));

        dates.Should().NotContain(d => d.TaxType == TaxType.Vat);
        dates.Should().OnlyContain(d => d.DueDate >= new DateOnly(2026, 4, 21)
            && d.DueDate <= new DateOnly(2026, 6, 30));
    }

    [Fact]
    public void DueDatesIn_InvertedRange_ReturnsEmpty()
    {
        var (calendar, _) = BuildSut();

        calendar.DueDatesIn(new DateOnly(2026, 6, 1), new DateOnly(2026, 1, 1)).Should().BeEmpty();
    }

    [Fact]
    public async Task EstimateFromRevenue_CalculatesCorrectly()
    {
        var (calendar, repo) = BuildSut();
        repo.VatTotals = new VatTotals(OutputVat: 5000m, InputVat: 1200m);
        repo.NetRevenue = 120_000m;

        var obligations = await calendar.GenerateAsync(
            BusinessId, new DateOnly(2026, 4, 1), new DateOnly(2026, 7, 31));

        // The window spans two filings (Q1 on 20 Apr, Q2 on 20 Jul); both draw
        // on the same stubbed totals.
        var vat = obligations.Where(o => o.TaxType == TaxType.Vat).ToList();
        vat.Should().HaveCount(2);
        vat.Should().OnlyContain(o => o.EstimatedAmount == 3800m);

        // Revenue * 22%, spread across three instalments.
        var incomeTax = obligations.First(o => o.TaxType == TaxType.IncomeTax);
        incomeTax.EstimatedAmount.Should().Be(Math.Round(120_000m * 0.22m / 3m, 2));

        var prepayment = obligations.First(o => o.TaxType == TaxType.TaxPrepayment);
        prepayment.EstimatedAmount.Should().Be(Math.Round(120_000m * 0.22m * 0.80m / 3m, 2));

        obligations.Single(o => o.TaxType == TaxType.ProfessionalTax)
            .EstimatedAmount.Should().Be(GreekTaxCalendar.ProfessionalTaxAmount);

        obligations.Should().OnlyContain(o => o.BusinessId == BusinessId);
    }

    [Fact]
    public async Task VatCredit_FloorsAtZero()
    {
        var (calendar, repo) = BuildSut();

        // More input than output VAT is a credit carried forward, not a refund due.
        repo.VatTotals = new VatTotals(OutputVat: 500m, InputVat: 2000m);

        var obligations = await calendar.GenerateAsync(
            BusinessId, new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30));

        obligations.Single(o => o.TaxType == TaxType.Vat).EstimatedAmount.Should().Be(0m);
    }

    [Fact]
    public async Task PayrollTaxes_EstimateZeroBecauseRoivoCannotSeePayroll()
    {
        var (calendar, repo) = BuildSut();
        repo.NetRevenue = 500_000m;

        var obligations = await calendar.GenerateAsync(
            BusinessId, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));

        obligations.Single(o => o.TaxType == TaxType.WithholdingTax).EstimatedAmount.Should().Be(0m);
        obligations.Single(o => o.TaxType == TaxType.SocialSecurity).EstimatedAmount.Should().Be(0m);
    }

    [Theory]
    [InlineData("2026-Q1", "2026-01-01", "2026-03-31")]
    [InlineData("2026-Q2", "2026-04-01", "2026-06-30")]
    [InlineData("2026-Q3", "2026-07-01", "2026-09-30")]
    [InlineData("2026-Q4", "2026-10-01", "2026-12-31")]
    public void QuarterRange_MapsPeriodToCalendarQuarter(string period, string expectedFrom, string expectedTo)
    {
        var (from, to) = GreekTaxCalendar.QuarterRange(period);

        from.Should().Be(DateOnly.Parse(expectedFrom));
        to.Should().Be(DateOnly.Parse(expectedTo));
    }
}
