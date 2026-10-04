namespace Roivo.Core.Domain.Enums;

/// <summary>How often a business files its ΦΠΑ return.</summary>
/// <remarks>
/// Greek law keys this to turnover: above the threshold a business files
/// monthly, below it quarterly. Roivo stores the choice rather than deriving it,
/// because the switch takes effect from a tax year the accountant decides, not
/// the moment revenue crosses the line.
/// </remarks>
public enum VatFrequency
{
    /// <summary>Due the 20th of the month after each quarter closes.</summary>
    Quarterly = 1,

    /// <summary>Due the 20th of the following month.</summary>
    Monthly = 2,
}
