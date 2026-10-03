namespace Roivo.Core.Domain.Enums;

/// <summary>
/// Greek tax and contribution obligations Roivo projects onto the cashflow
/// forecast. Values are stable — the audit trail and stored obligations
/// reference the string form.
/// </summary>
public enum TaxType
{
    /// <summary>ΦΠΑ. Quarterly for the small businesses Roivo targets.</summary>
    Vat = 1,

    /// <summary>Φόρος Εισοδήματος. Annual, settled in instalments.</summary>
    IncomeTax = 2,

    /// <summary>Παρακρατούμενος Φόρος. Monthly.</summary>
    WithholdingTax = 3,

    /// <summary>ΕΦΚΑ contributions. Monthly.</summary>
    SocialSecurity = 4,

    /// <summary>Τέλος Επιτηδεύματος. Annual, alongside income tax.</summary>
    ProfessionalTax = 5,

    /// <summary>Προκαταβολή Φόρου. Advance on next year, with the income-tax instalments.</summary>
    TaxPrepayment = 6,

    /// <summary>ΕΝΦΙΑ. Annual property tax, settled in monthly instalments from September.</summary>
    PropertyTax = 7,
}
