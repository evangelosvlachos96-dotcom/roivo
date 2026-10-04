namespace Roivo.Application.Features.Accountant.Services;

/// <summary>
/// Alert discriminators. The handler emits these; the UI maps each to its own
/// localized text, so no Greek crosses the Application boundary.
/// </summary>
public static class AccountantAlertType
{
    /// <summary>The stored projection goes below zero inside the horizon.</summary>
    public const string NegativeBalancePredicted = "NegativeBalancePredicted";

    /// <summary>An unpaid tax obligation falls due within the week.</summary>
    public const string TaxDueSoon = "TaxDueSoon";

    /// <summary>The banking sync is on a failure streak.</summary>
    public const string BankingSyncFailed = "BankingSyncFailed";

    /// <summary>AADE sync is failing in a way that needs user action.</summary>
    public const string AadeSyncFailed = "AadeSyncFailed";

    /// <summary>Fewer than half the window's invoices are accounted for.</summary>
    public const string LowReconciliationRate = "LowReconciliationRate";
}
