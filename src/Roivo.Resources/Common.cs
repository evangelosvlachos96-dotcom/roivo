namespace Roivo.Resources;

/// <summary>
/// Generic UI strings used across multiple domains: button labels, statuses,
/// confirmation prompts that aren't tied to a specific feature.
/// </summary>
public static class Common
{
    // Buttons
    public static string SaveButton => Strings.Get("Common.SaveButton", "Αποθήκευση");
    public static string CancelButton => Strings.Get("Common.CancelButton", "Άκυρο");
    public static string DeleteButton => Strings.Get("Common.DeleteButton", "Διαγραφή");
    public static string ConfirmButton => Strings.Get("Common.ConfirmButton", "Επιβεβαίωση");
    public static string BackButton => Strings.Get("Common.BackButton", "Πίσω");
    public static string CloseButton => Strings.Get("Common.CloseButton", "Κλείσιμο");
    public static string YesButton => Strings.Get("Common.YesButton", "Ναι");
    public static string NoButton => Strings.Get("Common.NoButton", "Όχι");
    public static string RetryButton => Strings.Get("Common.RetryButton", "Δοκιμή ξανά");

    // States
    public static string Loading => Strings.Get("Common.Loading", "Φόρτωση...");
    public static string Saving => Strings.Get("Common.Saving", "Αποθήκευση...");
    public static string NoData => Strings.Get("Common.NoData", "Δεν υπάρχουν δεδομένα");
    public static string Required => Strings.Get("Common.Required", "Υποχρεωτικό πεδίο");

    // Generic errors
    public static string Error_Generic => Strings.Get("Common.Error_Generic", "Παρουσιάστηκε σφάλμα. Δοκιμάστε ξανά.");
    public static string Error_Forbidden => Strings.Get("Common.Error_Forbidden", "Δεν έχετε δικαίωμα για αυτή την ενέργεια.");
    public static string Error_NotFound => Strings.Get("Common.Error_NotFound", "Δεν βρέθηκε.");
    public static string Error_ValidationFailed => Strings.Get("Common.Error_ValidationFailed", "Παρακαλώ διορθώστε τα σφάλματα στη φόρμα.");

    // Navigation
    public static string Nav_Dashboard => Strings.Get("Common.Nav_Dashboard", "Πίνακας ελέγχου");
    public static string Nav_Businesses => Strings.Get("Common.Nav_Businesses", "Επιχειρήσεις");
    public static string Nav_Login => Strings.Get("Common.Nav_Login", "Σύνδεση");
    public static string Nav_Register => Strings.Get("Common.Nav_Register", "Εγγραφή");
    public static string Nav_BrandName => Strings.Get("Common.Nav_BrandName", "Roivo");

    // Home page
    public static string Home_HeroSubtitle => Strings.Get("Common.Home_HeroSubtitle", "Έλεγχος ταμειακής ροής. Χωρίς λογιστικό φύλλο.");

    public static string RequiredShort => Strings.Get("Common.RequiredShort", "Υποχρεωτικό");
    // Endonyms: the language switcher labels the *target* language in that
    // language, so these stay pinned (_El/_En) rather than being translated.
    public static string LanguageName_El => Strings.Get("Common.LanguageName_El", "Ελληνικά");
    public static string LanguageName_En => Strings.Get("Common.LanguageName_En", "English");

    public static string Nav_Menu => Strings.Get("Common.Nav_Menu", "Μενού πλοήγησης");

    /// <summary>English overrides. Keys absent here fall back to Greek.</summary>
    static Common() => Strings.RegisterEnglish(new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Common.SaveButton"] = "Save",
        ["Common.CancelButton"] = "Cancel",
        ["Common.DeleteButton"] = "Delete",
        ["Common.ConfirmButton"] = "Confirm",
        ["Common.BackButton"] = "Back",
        ["Common.CloseButton"] = "Close",
        ["Common.YesButton"] = "Yes",
        ["Common.NoButton"] = "No",
        ["Common.RetryButton"] = "Retry",
        ["Common.Loading"] = "Loading…",
        ["Common.Saving"] = "Saving…",
        ["Common.NoData"] = "No data",
        ["Common.Required"] = "Required field",
        ["Common.Error_Generic"] = "Something went wrong. Please try again.",
        ["Common.Error_Forbidden"] = "You do not have permission for this action.",
        ["Common.Error_NotFound"] = "Not found.",
        ["Common.Error_ValidationFailed"] = "Please correct the errors in the form.",
        ["Common.Nav_Dashboard"] = "Dashboard",
        ["Common.Nav_Businesses"] = "Businesses",
        ["Common.Nav_Login"] = "Sign in",
        ["Common.Nav_Register"] = "Sign up",
        ["Common.Nav_BrandName"] = "Roivo",
        ["Common.Home_HeroSubtitle"] = "Cashflow control. Without a spreadsheet.",
        ["Common.RequiredShort"] = "Required",
        ["Common.Nav_Menu"] = "Navigation menu",
    });
}
