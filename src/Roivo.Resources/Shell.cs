namespace Roivo.Resources;

/// <summary>
/// Chrome for the product shell: sidebar navigation, header, theme switch.
/// Separate from <see cref="Common"/> because these strings belong to one
/// layout rather than being shared vocabulary.
/// </summary>
public static class Shell
{
    // Sidebar section headers. Uppercase in the design, and written uppercase
    // here rather than via text-transform so the Greek accents are dropped the
    // way Greek typography expects (ΠΑΡΑΚΟΛΟΥΘΗΣΗ, not ΠΑΡΑΚΟΛΟΎΘΗΣΗ).
    public static string Group_Workspace => Strings.Get("Shell.Group_Workspace", "ΧΩΡΟΣ ΕΡΓΑΣΙΑΣ");
    public static string Group_Monitoring => Strings.Get("Shell.Group_Monitoring", "ΠΑΡΑΚΟΛΟΥΘΗΣΗ");
    public static string Group_Tasks => Strings.Get("Shell.Group_Tasks", "ΕΡΓΑΣΙΕΣ");
    public static string Group_Management => Strings.Get("Shell.Group_Management", "ΔΙΑΧΕΙΡΙΣΗ");

    public static string Nav_Overview => Strings.Get("Shell.Nav_Overview", "Επισκόπηση");
    public static string Nav_Cashflow => Strings.Get("Shell.Nav_Cashflow", "Ταμειακές ροές");
    public static string Nav_Reconciliation => Strings.Get("Shell.Nav_Reconciliation", "Συμφωνίες");
    public static string Nav_Obligations => Strings.Get("Shell.Nav_Obligations", "Υποχρεώσεις");
    public static string Nav_Reports => Strings.Get("Shell.Nav_Reports", "Αναφορές");
    public static string Nav_Connections => Strings.Get("Shell.Nav_Connections", "Συνδέσεις");
    public static string Nav_Help => Strings.Get("Shell.Nav_Help", "Βοήθεια");
    public static string Nav_Settings => Strings.Get("Shell.Nav_Settings", "Ρυθμίσεις");

    public static string Nav_Label => Strings.Get("Shell.Nav_Label", "Κύρια πλοήγηση");
    public static string AccountantWorkspace => Strings.Get("Shell.AccountantWorkspace", "Λογιστικό γραφείο");

    public static string BrandTagline => Strings.Get("Shell.BrandTagline", "Cashflow intelligence");

    // Header
    public static string SearchPlaceholder => Strings.Get("Shell.SearchPlaceholder", "Αναζήτηση...");
    public static string SearchLabel => Strings.Get("Shell.SearchLabel", "Καθολική αναζήτηση");
    public static string Notifications => Strings.Get("Shell.Notifications", "Ειδοποιήσεις");
    public static string Theme_ToDark => Strings.Get("Shell.Theme_ToDark", "Ενεργοποίηση σκοτεινού θέματος");
    public static string Theme_ToLight => Strings.Get("Shell.Theme_ToLight", "Ενεργοποίηση φωτεινού θέματος");
    public static string OpenMenu => Strings.Get("Shell.OpenMenu", "Άνοιγμα μενού");

    public static string Placeholder_Heading => Strings.Get("Shell.Placeholder_Heading", "Σε εξέλιξη");
    public static string Placeholder_Body => Strings.Get("Shell.Placeholder_Body", "Αυτή η ενότητα σχεδιάζεται αυτή τη στιγμή. Τα δεδομένα σας είναι ασφαλή και διαθέσιμα στις υπάρχουσες σελίδες.");
    public static string Placeholder_Cashflow => Strings.Get("Shell.Placeholder_Cashflow", "Εισροές, εκροές και πρόβλεψη 90 ημερών.");
    public static string Placeholder_Reconciliation => Strings.Get("Shell.Placeholder_Reconciliation", "Αντιστοίχιση παραστατικών με τραπεζικές κινήσεις.");
    public static string Placeholder_Obligations => Strings.Get("Shell.Placeholder_Obligations", "ΦΠΑ, ΕΦΚΑ και κάθε φορολογική προθεσμία.");
    public static string Placeholder_Reports => Strings.Get("Shell.Placeholder_Reports", "Συγκεντρωτικές αναφορές για όλο το πελατολόγιο.");
    public static string Placeholder_Connections => Strings.Get("Shell.Placeholder_Connections", "Τράπεζες και AADE myDATA.");
    public static string Placeholder_Settings => Strings.Get("Shell.Placeholder_Settings", "Προφίλ, επιχείρηση, χρήστες και εμφάνιση.");
    public static string Placeholder_Help => Strings.Get("Shell.Placeholder_Help", "Οδηγοί και υποστήριξη.");

    /// <summary>English overrides. Keys absent here fall back to Greek.</summary>
    static Shell() => Strings.RegisterEnglish(new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Shell.Group_Workspace"] = "WORKSPACE",
        ["Shell.Group_Monitoring"] = "MONITORING",
        ["Shell.Group_Tasks"] = "TASKS",
        ["Shell.Group_Management"] = "MANAGEMENT",
        ["Shell.Nav_Overview"] = "Overview",
        ["Shell.Nav_Cashflow"] = "Cashflow",
        ["Shell.Nav_Reconciliation"] = "Reconciliation",
        ["Shell.Nav_Obligations"] = "Obligations",
        ["Shell.Nav_Reports"] = "Reports",
        ["Shell.Nav_Connections"] = "Connections",
        ["Shell.Nav_Help"] = "Help",
        ["Shell.Nav_Settings"] = "Settings",
        ["Shell.Nav_Label"] = "Main navigation",
        ["Shell.AccountantWorkspace"] = "Accountancy practice",
        ["Shell.BrandTagline"] = "Cashflow intelligence",
        ["Shell.SearchPlaceholder"] = "Search...",
        ["Shell.SearchLabel"] = "Global search",
        ["Shell.Notifications"] = "Notifications",
        ["Shell.Theme_ToDark"] = "Switch to dark theme",
        ["Shell.Theme_ToLight"] = "Switch to light theme",
        ["Shell.OpenMenu"] = "Open menu",
        ["Shell.Placeholder_Heading"] = "In progress",
        ["Shell.Placeholder_Body"] = "This section is being redesigned. Your data is safe and still available on the existing pages.",
        ["Shell.Placeholder_Cashflow"] = "Inflows, outflows and the 90-day forecast.",
        ["Shell.Placeholder_Reconciliation"] = "Matching invoices against bank transactions.",
        ["Shell.Placeholder_Obligations"] = "VAT, social security and every tax deadline.",
        ["Shell.Placeholder_Reports"] = "Consolidated reports across your client book.",
        ["Shell.Placeholder_Connections"] = "Banks and AADE myDATA.",
        ["Shell.Placeholder_Settings"] = "Profile, business, users and appearance.",
        ["Shell.Placeholder_Help"] = "Guides and support.",
    });
}
