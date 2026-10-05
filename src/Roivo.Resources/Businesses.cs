namespace Roivo.Resources;

/// <summary>User-facing strings for business management UI.</summary>
public static class Businesses
{
    public static string PageTitle_List => Strings.Get("Businesses.PageTitle_List", "Επιχειρήσεις");
    public static string PageTitle_Create => Strings.Get("Businesses.PageTitle_Create", "Δημιουργία επιχείρησης");
    public static string PageTitle_Edit => Strings.Get("Businesses.PageTitle_Edit", "Επεξεργασία επιχείρησης");

    public static string AddButton => Strings.Get("Businesses.AddButton", "Προσθήκη επιχείρησης");
    public static string EditButton => Strings.Get("Businesses.EditButton", "Επεξεργασία");
    public static string DeactivateButton => Strings.Get("Businesses.DeactivateButton", "Απενεργοποίηση");
    public static string ReactivateButton => Strings.Get("Businesses.ReactivateButton", "Επανενεργοποίηση");

    public static string NameLabel => Strings.Get("Businesses.NameLabel", "Όνομα");
    public static string AfmLabel => Strings.Get("Businesses.AfmLabel", "ΑΦΜ");
    public static string KadLabel => Strings.Get("Businesses.KadLabel", "ΚΑΔ");
    public static string AddressLabel => Strings.Get("Businesses.AddressLabel", "Διεύθυνση");
    public static string StatusLabel => Strings.Get("Businesses.StatusLabel", "Κατάσταση");
    public static string TypeLabel => Strings.Get("Businesses.TypeLabel", "Τύπος");

    public static string Status_Active => Strings.Get("Businesses.Status_Active", "Ενεργή");
    public static string Status_Inactive => Strings.Get("Businesses.Status_Inactive", "Ανενεργή");

    public static string Confirm_Deactivate => Strings.Get("Businesses.Confirm_Deactivate", "Είστε σίγουρος ότι θέλετε να απενεργοποιήσετε την επιχείρηση \"{0}\";");
    public static string Confirm_Reactivate => Strings.Get("Businesses.Confirm_Reactivate", "Επανενεργοποίηση της επιχείρησης \"{0}\";");

    public static string InactiveDuplicateDetected_Heading => Strings.Get("Businesses.InactiveDuplicateDetected_Heading", "Υπάρχει ήδη ανενεργή επιχείρηση με αυτό το ΑΦΜ");
    public static string InactiveDuplicateDetected_Description => Strings.Get("Businesses.InactiveDuplicateDetected_Description", "Η επιχείρηση \"{0}\" με ΑΦΜ {1} είχε απενεργοποιηθεί στις {2}. Μπορείτε να την επανενεργοποιήσετε.");
    public static string InactiveDuplicateDetected_BackToForm => Strings.Get("Businesses.InactiveDuplicateDetected_BackToForm", "Πίσω στο φόρμα");

    public static string ActiveDuplicate_Error => Strings.Get("Businesses.ActiveDuplicate_Error", "Υπάρχει ήδη ενεργή επιχείρηση με αυτό το ΑΦΜ.");
    public static string InvalidAfm_Error => Strings.Get("Businesses.InvalidAfm_Error", "Μη έγκυρο ΑΦΜ.");
    public static string Forbidden_Error => Strings.Get("Businesses.Forbidden_Error", "Δεν έχετε τα απαραίτητα δικαιώματα για αυτή την ενέργεια.");
    public static string NotFound_Error => Strings.Get("Businesses.NotFound_Error", "Η επιχείρηση δεν βρέθηκε.");

    public static string AadeColumn_Header => Strings.Get("Businesses.AadeColumn_Header", "AADE");
    public static string ManageAadeConnection => Strings.Get("Businesses.ManageAadeConnection", "Διαχείριση σύνδεσης AADE");
    public static string ManageBankingConnection => Strings.Get("Businesses.ManageBankingConnection", "Διαχείριση τραπεζικής σύνδεσης");

    // Empty states
    public static string EmptyList_Accountant => Strings.Get("Businesses.EmptyList_Accountant", "Δεν έχετε προσθέσει ακόμα επιχειρήσεις. Κάντε κλικ στο \"Προσθήκη επιχείρησης\" για να ξεκινήσετε.");
    public static string EmptyList_BusinessOwner => Strings.Get("Businesses.EmptyList_BusinessOwner", "Δεν έχετε προσθέσει ακόμα την επιχείρησή σας.");

    /// <summary>English overrides. Keys absent here fall back to Greek.</summary>
    static Businesses() => Strings.RegisterEnglish(new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Businesses.PageTitle_List"] = "Businesses",
        ["Businesses.PageTitle_Create"] = "New business",
        ["Businesses.PageTitle_Edit"] = "Edit business",
        ["Businesses.AddButton"] = "Add business",
        ["Businesses.EditButton"] = "Edit",
        ["Businesses.DeactivateButton"] = "Deactivate",
        ["Businesses.ReactivateButton"] = "Reactivate",
        ["Businesses.NameLabel"] = "Name",
        ["Businesses.AfmLabel"] = "VAT number",
        ["Businesses.KadLabel"] = "Activity code (KAD)",
        ["Businesses.AddressLabel"] = "Address",
        ["Businesses.StatusLabel"] = "Status",
        ["Businesses.TypeLabel"] = "Type",
        ["Businesses.Status_Active"] = "Active",
        ["Businesses.Status_Inactive"] = "Inactive",
        ["Businesses.Confirm_Deactivate"] = "Are you sure you want to deactivate the business \"{0}\"?",
        ["Businesses.Confirm_Reactivate"] = "Reactivate the business \"{0}\"?",
        ["Businesses.InactiveDuplicateDetected_Heading"] = "An inactive business with this VAT number already exists",
        ["Businesses.InactiveDuplicateDetected_Description"] = "The business \"{0}\" with VAT number {1} was deactivated on {2}. You can reactivate it.",
        ["Businesses.InactiveDuplicateDetected_BackToForm"] = "Back to the form",
        ["Businesses.ActiveDuplicate_Error"] = "An active business with this VAT number already exists.",
        ["Businesses.InvalidAfm_Error"] = "Invalid VAT number.",
        ["Businesses.Forbidden_Error"] = "You do not have the permissions required for this action.",
        ["Businesses.NotFound_Error"] = "Business not found.",
        ["Businesses.AadeColumn_Header"] = "AADE",
        ["Businesses.ManageAadeConnection"] = "Manage AADE connection",
        ["Businesses.ManageBankingConnection"] = "Manage bank connection",
        ["Businesses.EmptyList_Accountant"] = "You have not added any businesses yet. Click \"Add business\" to get started.",
        ["Businesses.EmptyList_BusinessOwner"] = "You have not added your business yet.",
    });
}
