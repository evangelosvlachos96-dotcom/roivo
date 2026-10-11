using System.Globalization;
using Roivo.Resources;

namespace Roivo.Web.Components.Shared;

/// <summary>
/// Turns a tax obligation's stored period into something a person reads.
/// </summary>
/// <remarks>
/// <para>
/// <c>TaxObligation.Period</c> is a machine key — <c>2026-09</c>, <c>2026-Q3</c>,
/// <c>2025-I3</c> — because that is what the calendar matches obligations on and
/// what the ΑΑΔΕ sync reconciles against. It was never meant to be read, and on
/// screen it looked like a serial number.
/// </para>
/// <para>
/// Anything that does not match a known shape is returned untouched. Periods
/// written by hand or by an older seed are already prose, and mangling them
/// would be worse than leaving them.
/// </para>
/// </remarks>
public static class PeriodLabels
{
    public static string For(string? period)
    {
        if (string.IsNullOrWhiteSpace(period)) return "";

        var text = period.Trim();
        var dash = text.IndexOf('-', StringComparison.Ordinal);

        // A bare year: Τέλος Επιτηδεύματος settles one, with no subdivision.
        // Nothing to format, and nothing else is shaped this way.
        if (dash < 0) return text;

        var head = text[..dash];
        var tail = text[(dash + 1)..];

        if (!int.TryParse(head, NumberStyles.None, CultureInfo.InvariantCulture, out var year))
            return text;

        // "2026-09" — a calendar month, which the culture already knows how to
        // name in either language.
        if (int.TryParse(tail, NumberStyles.None, CultureInfo.InvariantCulture, out var month)
            && month is >= 1 and <= 12)
        {
            return new DateOnly(year, month, 1).ToString("MMMM yyyy", Strings.FormatCulture);
        }

        if (tail.Length < 2
            || !int.TryParse(tail[1..], NumberStyles.None, CultureInfo.InvariantCulture, out var index))
        {
            return text;
        }

        return char.ToUpperInvariant(tail[0]) switch
        {
            // ΦΠΑ for a quarter.
            'Q' => Cashflow.Period_Quarter.Format(index, year),

            // Φόρος Εισοδήματος and ΕΝΦΙΑ both settle in instalments; which tax
            // it is has already been said beside this.
            'I' or 'E' => Cashflow.Period_Instalment.Format(index, year),

            'P' => Cashflow.Period_Prepayment.Format(index, year),

            _ => text,
        };
    }
}
