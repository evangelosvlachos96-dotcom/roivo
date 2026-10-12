namespace Roivo.Resources;

/// <summary>
/// User-facing strings for the signed-in landing dashboard.
/// {N} placeholders are positional arguments for <c>string.Format</c>.
/// </summary>
public static class Dashboard
{
    public static string Welcome => Strings.Get("Dashboard.Welcome", "Καλωσήρθες, {0}");
    public static string YourOrganization => Strings.Get("Dashboard.YourOrganization", "Ο οργανισμός σου");
    public static string TenantLabel => Strings.Get("Dashboard.TenantLabel", "Οργανισμός");
    public static string TypeLabel => Strings.Get("Dashboard.TypeLabel", "Τύπος");
    public static string AfmLabel => Strings.Get("Dashboard.AfmLabel", "ΑΦΜ");

    public static string BusinessesHeading => Strings.Get("Dashboard.BusinessesHeading", "Επιχειρήσεις");
    public static string BusinessCountLabel => Strings.Get("Dashboard.BusinessCountLabel", "Κάτω από αυτόν τον οργανισμό:");
    public static string LogoutButton => Strings.Get("Dashboard.LogoutButton", "Αποσύνδεση");

    public static string Error_UserNotFound => Strings.Get("Dashboard.Error_UserNotFound", "Δεν βρέθηκε ο χρήστης.");
    public static string Error_NoTenantClaim => Strings.Get("Dashboard.Error_NoTenantClaim", "Δεν βρέθηκε οργανισμός στα στοιχεία σύνδεσής σου.");

    // TenantType enum labels. Kept here rather than on the enum so they stay
    // culture-aware; the enum itself is a Core concern with no resource access.
    public static string TenantType_Accountant => Strings.Get("Dashboard.TenantType_Accountant", "Λογιστής");
    public static string TenantType_Business => Strings.Get("Dashboard.TenantType_Business", "Ιδιοκτήτης επιχείρησης");
    public static string TenantType_Unknown => Strings.Get("Dashboard.TenantType_Unknown", "Άγνωστος τύπος");

    /// <summary>English overrides. Keys absent here fall back to Greek.</summary>
    static Dashboard() => Strings.RegisterEnglish(new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Dashboard.Welcome"] = "Welcome, {0}",
        ["Dashboard.YourOrganization"] = "Your organisation",
        ["Dashboard.TenantLabel"] = "Organisation",
        ["Dashboard.TypeLabel"] = "Type",
        ["Dashboard.AfmLabel"] = "VAT number",
        ["Dashboard.BusinessesHeading"] = "Businesses",
        ["Dashboard.BusinessCountLabel"] = "Under this organisation:",
        ["Dashboard.LogoutButton"] = "Sign out",
        ["Dashboard.Error_UserNotFound"] = "User not found.",
        ["Dashboard.Error_NoTenantClaim"] = "No organisation was found in your sign-in details.",
        ["Dashboard.TenantType_Accountant"] = "Accountant",
        ["Dashboard.TenantType_Business"] = "Business owner",
        ["Dashboard.TenantType_Unknown"] = "Unknown type",
    });
}
