using Roivo.Core.Domain.Enums;

namespace Roivo.Web.Components.Shared;

/// <summary>
/// Display names for the Greek tax types.
/// </summary>
/// <remarks>
/// Lives here rather than on <see cref="Roivo.Resources.Cashflow"/> because
/// Roivo.Resources deliberately references no other project, and this has to
/// switch on a Core enum. One definition shared by every page that lists
/// obligations, so a new TaxType cannot be labelled two different ways.
/// </remarks>
public static class TaxLabels
{
    public static string For(TaxType taxType) => taxType switch
    {
        TaxType.Vat => Roivo.Resources.Cashflow.Tax_Vat,
        TaxType.IncomeTax => Roivo.Resources.Cashflow.Tax_IncomeTax,
        TaxType.WithholdingTax => Roivo.Resources.Cashflow.Tax_WithholdingTax,
        TaxType.SocialSecurity => Roivo.Resources.Cashflow.Tax_SocialSecurity,
        TaxType.ProfessionalTax => Roivo.Resources.Cashflow.Tax_ProfessionalTax,
        TaxType.TaxPrepayment => Roivo.Resources.Cashflow.Tax_TaxPrepayment,
        TaxType.PropertyTax => Roivo.Resources.Cashflow.Tax_PropertyTax,
        _ => Roivo.Resources.Cashflow.TaxCalendar,
    };
}
