namespace Roivo.Resources;

/// <summary>
/// Generic UI strings used across multiple domains: button labels, statuses,
/// confirmation prompts that aren't tied to a specific feature.
/// </summary>
public static class Common
{
    // Buttons
    public const string SaveButton = "Αποθήκευση";
    public const string CancelButton = "Άκυρο";
    public const string DeleteButton = "Διαγραφή";
    public const string ConfirmButton = "Επιβεβαίωση";
    public const string BackButton = "Πίσω";
    public const string CloseButton = "Κλείσιμο";
    public const string YesButton = "Ναι";
    public const string NoButton = "Όχι";
    public const string RetryButton = "Δοκιμή ξανά";

    // States
    public const string Loading = "Φόρτωση...";
    public const string Saving = "Αποθήκευση...";
    public const string NoData = "Δεν υπάρχουν δεδομένα";
    public const string Required = "Υποχρεωτικό πεδίο";

    // Generic errors
    public const string Error_Generic = "Παρουσιάστηκε σφάλμα. Δοκιμάστε ξανά.";
    public const string Error_Forbidden = "Δεν έχετε δικαίωμα για αυτή την ενέργεια.";
    public const string Error_NotFound = "Δεν βρέθηκε.";
    public const string Error_ValidationFailed = "Παρακαλώ διορθώστε τα σφάλματα στη φόρμα.";

    // Navigation
    public const string Nav_Dashboard = "Πίνακας ελέγχου";
    public const string Nav_Businesses = "Επιχειρήσεις";
    public const string Nav_Login = "Σύνδεση";
    public const string Nav_Register = "Εγγραφή";
    public const string Nav_BrandName = "Roivo";

    // Home page
    public const string Home_HeroSubtitle = "Έλεγχος ταμειακής ροής. Χωρίς λογιστικό φύλλο.";
}
