using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Features.Cashflow.Services;

/// <summary>
/// Generates the Greek tax and contribution obligations falling due in a window.
/// </summary>
public interface IGreekTaxCalendar
{
    /// <summary>
    /// Builds the obligations due between the two dates, estimating amounts from
    /// the business's own invoice history. Returns unsaved entities; the caller
    /// decides whether to persist them.
    /// </summary>
    Task<IReadOnlyList<TaxObligation>> GenerateAsync(
        Guid businessId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Statutory due dates only, with no estimates and no I/O.
    /// </summary>
    /// <param name="vatFrequency">
    /// How often the business files ΦΠΑ. Defaults to quarterly, which is what most
    /// of the businesses Roivo serves do; a caller that knows the business
    /// should pass its actual setting.
    /// </param>
    /// <param name="includePropertyTax">
    /// Whether to emit the ΕΝΦΙΑ instalments. Off by default: the instalments are
    /// statutory, but Roivo only knows a business owns property when the owner
    /// has told it a value, so the default assumes it does not.
    /// </param>
    IReadOnlyList<TaxDueDate> DueDatesIn(
        DateOnly from,
        DateOnly to,
        VatFrequency vatFrequency = VatFrequency.Quarterly,
        bool includePropertyTax = false);
}

/// <summary>A statutory due date and the period it settles.</summary>
public sealed record TaxDueDate(TaxType TaxType, DateOnly DueDate, string Period);

/// <summary>
/// The Greek filing calendar as it applies to the small businesses Roivo serves.
/// </summary>
/// <remarks>
/// Dates are the statutory ones. AADE moves deadlines by decision most years —
/// usually later, never earlier — so treating these as the earliest plausible
/// date keeps the forecast conservative. Rates are the standard ones and are
/// deliberately coarse: this projects cash, it does not file returns.
/// </remarks>
public sealed class GreekTaxCalendar : IGreekTaxCalendar
{
    /// <summary>Standard VAT rate. Reduced rates exist; the estimate uses actual invoice VAT where it can.</summary>
    public const decimal StandardVatRate = 0.24m;

    /// <summary>Flat corporate income-tax rate used for the projection.</summary>
    public const decimal IncomeTaxRate = 0.22m;

    /// <summary>Advance on next year's income tax, as a share of the assessed amount.</summary>
    public const decimal PrepaymentRate = 0.80m;

    /// <summary>Τέλος Επιτηδεύματος for a standard business. A flat annual fee, not a rate.</summary>
    public const decimal ProfessionalTaxAmount = 650m;

    /// <summary>
    /// Share of estimated property value used as the annual ΕΝΦΙΑ projection.
    /// </summary>
    /// <remarks>
    /// This is a projection, not a calculation. Real ΕΝΦΙΑ is assessed per
    /// property from the objective value, the zone price, the floor, the age
    /// and the frontage, plus a supplementary band on larger holdings — none of
    /// which Roivo can see, having no access to the property register (Ε9). A
    /// flat 0.5% of the owner's own value estimate lands in the upper half of
    /// what typical holdings actually pay, which keeps the cash projection on
    /// the safe side. It is a placeholder for the real assessment the user
    /// should overwrite it with, not an attempt to reproduce one.
    /// </remarks>
    public const decimal CoarsePropertyTaxProjectionRate = 0.005m;

    /// <summary>Monthly ΕΝΦΙΑ instalments, September through January.</summary>
    public const int PropertyTaxInstalments = 5;

    /// <summary>Month the first ΕΝΦΙΑ instalment falls due.</summary>
    private const int FirstPropertyTaxMonth = 9;

    /// <summary>Day of the month that VAT and withholding tax fall due.</summary>
    private const int FilingDay = 20;

    /// <summary>Months the income-tax instalments fall due.</summary>
    private static readonly int[] IncomeTaxInstalmentMonths = [7, 9, 11];

    private readonly ICashflowRepository _repository;

    public GreekTaxCalendar(ICashflowRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public IReadOnlyList<TaxDueDate> DueDatesIn(
        DateOnly from,
        DateOnly to,
        VatFrequency vatFrequency = VatFrequency.Quarterly,
        bool includePropertyTax = false)
    {
        if (to < from)
            return [];

        var dates = new List<TaxDueDate>();

        // Walk month by month from the start of the first month to the end of
        // the last, so an obligation falling anywhere inside the window is found
        // regardless of where the window boundaries sit within a month.
        var cursor = new DateOnly(from.Year, from.Month, 1);
        var end = new DateOnly(to.Year, to.Month, 1);

        while (cursor <= end)
        {
            foreach (var due in DueDatesForMonth(cursor.Year, cursor.Month, vatFrequency, includePropertyTax))
            {
                if (due.DueDate >= from && due.DueDate <= to)
                    dates.Add(due);
            }

            cursor = cursor.AddMonths(1);
        }

        return [.. dates.OrderBy(d => d.DueDate).ThenBy(d => d.TaxType)];
    }

    public async Task<IReadOnlyList<TaxObligation>> GenerateAsync(
        Guid businessId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var profile = await _repository
            .GetTaxProfileAsync(businessId, cancellationToken)
            .ConfigureAwait(false)
            ?? BusinessTaxProfile.Default;

        var dueDates = DueDatesIn(
            from,
            to,
            profile.VatFrequency,
            includePropertyTax: profile.EstimatedPropertyValue.HasValue);

        if (dueDates.Count == 0)
            return [];

        var obligations = new List<TaxObligation>(dueDates.Count);

        foreach (var due in dueDates)
        {
            var amount = await EstimateAsync(businessId, due, profile, cancellationToken).ConfigureAwait(false);
            obligations.Add(TaxObligation.Create(businessId, due.TaxType, due.DueDate, amount, due.Period));
        }

        return obligations;
    }

    /// <summary>All obligations statutorily due in the given month.</summary>
    private static IEnumerable<TaxDueDate> DueDatesForMonth(
        int year,
        int month,
        VatFrequency vatFrequency,
        bool includePropertyTax)
    {
        // The month a filing made this month settles. January's settles the
        // previous December, which is why the year steps back with it.
        var withheldMonth = month == 1 ? new DateOnly(year - 1, 12, 1) : new DateOnly(year, month - 1, 1);

        // ΦΠΑ: one frequency or the other, never both. Quarterly filers pay on
        // the 20th of the month after the quarter closes; monthly filers on the
        // 20th of the month after the month closes.
        if (vatFrequency == VatFrequency.Monthly)
        {
            yield return new TaxDueDate(
                TaxType.Vat,
                new DateOnly(year, month, FilingDay),
                $"{withheldMonth.Year:D4}-{withheldMonth.Month:D2}");
        }
        else if (month is 1 or 4 or 7 or 10)
        {
            var (quarterYear, quarter) = month == 1 ? (year - 1, 4) : (year, (month - 1) / 3);
            yield return new TaxDueDate(
                TaxType.Vat,
                new DateOnly(year, month, FilingDay),
                $"{quarterYear}-Q{quarter}");
        }

        // Παρακρατούμενος Φόρος: monthly, on the 20th of the following month.
        yield return new TaxDueDate(
            TaxType.WithholdingTax,
            new DateOnly(year, month, FilingDay),
            $"{withheldMonth.Year:D4}-{withheldMonth.Month:D2}");

        // ΕΦΚΑ: monthly, by the last day of the following month. Contributions
        // are due on a calendar day, not a business day — the statutory wording
        // is the month end, so a weekend does not move it.
        yield return new TaxDueDate(
            TaxType.SocialSecurity,
            new DateOnly(year, month, DateTime.DaysInMonth(year, month)),
            $"{withheldMonth.Year:D4}-{withheldMonth.Month:D2}");

        // Φόρος Εισοδήματος and its prepayment: three instalments, settling the
        // prior tax year. Τέλος Επιτηδεύματος rides with the first instalment.
        if (IncomeTaxInstalmentMonths.Contains(month))
        {
            var taxYear = year - 1;
            var instalment = Array.IndexOf(IncomeTaxInstalmentMonths, month) + 1;

            yield return new TaxDueDate(
                TaxType.IncomeTax,
                new DateOnly(year, month, DateTime.DaysInMonth(year, month)),
                $"{taxYear}-I{instalment}");

            yield return new TaxDueDate(
                TaxType.TaxPrepayment,
                new DateOnly(year, month, DateTime.DaysInMonth(year, month)),
                $"{taxYear}-P{instalment}");

            if (instalment == 1)
            {
                yield return new TaxDueDate(
                    TaxType.ProfessionalTax,
                    new DateOnly(year, month, DateTime.DaysInMonth(year, month)),
                    $"{taxYear}");
            }
        }

        // ΕΝΦΙΑ: five monthly instalments, September through the following
        // January. The period year is the year the first instalment falls in,
        // so January's E5 settles the previous year's assessment. The statutory
        // date is the last working day of the month; month end stands in for it
        // because the gap is at most two days and the forecast is day-coarse.
        if (includePropertyTax && (month >= FirstPropertyTaxMonth || month == 1))
        {
            var (assessmentYear, instalment) = month == 1
                ? (year - 1, PropertyTaxInstalments)
                : (year, month - FirstPropertyTaxMonth + 1);

            yield return new TaxDueDate(
                TaxType.PropertyTax,
                new DateOnly(year, month, DateTime.DaysInMonth(year, month)),
                $"{assessmentYear}-E{instalment}");
        }
    }

    private async Task<decimal> EstimateAsync(
        Guid businessId,
        TaxDueDate due,
        BusinessTaxProfile profile,
        CancellationToken cancellationToken)
    {
        switch (due.TaxType)
        {
            case TaxType.Vat:
            {
                // The range has to match the period the filing settles, or a
                // monthly filer would be charged a whole quarter every month.
                var (from, to) = VatPeriodRange(due.Period);
                var totals = await _repository
                    .GetVatTotalsAsync(businessId, from, to, cancellationToken)
                    .ConfigureAwait(false);
                return Round(totals.Payable);
            }

            case TaxType.IncomeTax:
            {
                var (from, to) = YearRange(due.Period);
                var revenue = await _repository
                    .GetNetRevenueAsync(businessId, from, to, cancellationToken)
                    .ConfigureAwait(false);
                // Revenue stands in for taxable profit: Roivo sees invoices, not
                // the deductions that turn them into a tax base, so this is an
                // upper bound the user is expected to correct.
                return Round(Math.Max(0m, revenue) * IncomeTaxRate / IncomeTaxInstalmentMonths.Length);
            }

            case TaxType.TaxPrepayment:
            {
                var (from, to) = YearRange(due.Period);
                var revenue = await _repository
                    .GetNetRevenueAsync(businessId, from, to, cancellationToken)
                    .ConfigureAwait(false);
                return Round(Math.Max(0m, revenue) * IncomeTaxRate * PrepaymentRate / IncomeTaxInstalmentMonths.Length);
            }

            case TaxType.ProfessionalTax:
                return ProfessionalTaxAmount;

            case TaxType.PropertyTax:
                // No value means no obligation. DueDatesIn already withholds the
                // dates in that case, so this is the belt to that braces:
                // inventing a figure for property Roivo cannot see would be
                // worse than leaving ΕΝΦΙΑ out of the forecast entirely.
                return profile.EstimatedPropertyValue is not { } propertyValue
                    ? 0m
                    : Round(Math.Max(0m, propertyValue)
                        * CoarsePropertyTaxProjectionRate
                        / PropertyTaxInstalments);

            // Withholding and ΕΦΚΑ depend on payroll, which Roivo does not see.
            // Zero rather than a guess: a user-entered recurring category is the
            // honest way to get these into the forecast.
            case TaxType.WithholdingTax:
            case TaxType.SocialSecurity:
            default:
                return 0m;
        }
    }

    /// <summary>
    /// Calendar range a ΦΠΑ period covers, whether it is a quarter
    /// (<c>yyyy-Qn</c>) or a single month (<c>yyyy-MM</c>).
    /// </summary>
    public static (DateOnly From, DateOnly To) VatPeriodRange(string period)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(period);

        return period.Contains('Q', StringComparison.Ordinal)
            ? QuarterRange(period)
            : MonthRange(period);
    }

    /// <summary>Parses a <c>yyyy-MM</c> period into its calendar range.</summary>
    public static (DateOnly From, DateOnly To) MonthRange(string period)
    {
        var parts = period.Split('-');
        var year = int.Parse(parts[0]);
        var month = int.Parse(parts[1]);

        var from = new DateOnly(year, month, 1);
        return (from, from.AddMonths(1).AddDays(-1));
    }

    /// <summary>Parses a <c>yyyy-Qn</c> period into its calendar range.</summary>
    public static (DateOnly From, DateOnly To) QuarterRange(string period)
    {
        var parts = period.Split('-');
        var year = int.Parse(parts[0]);
        var quarter = int.Parse(parts[1].TrimStart('Q'));

        var startMonth = (quarter - 1) * 3 + 1;
        var from = new DateOnly(year, startMonth, 1);
        var to = from.AddMonths(3).AddDays(-1);
        return (from, to);
    }

    /// <summary>Parses the year out of a period and returns that whole calendar year.</summary>
    public static (DateOnly From, DateOnly To) YearRange(string period)
    {
        var year = int.Parse(period.Split('-')[0]);
        return (new DateOnly(year, 1, 1), new DateOnly(year, 12, 31));
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
