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

    public static string List_Subtitle => Strings.Get("Businesses.List_Subtitle", "Διαχείριση των επιχειρήσεων που εκπροσωπείτε.");
    public static string List_CardHeading => Strings.Get("Businesses.List_CardHeading", "Λίστα επιχειρήσεων");
    public static string EmptyState_Heading => Strings.Get("Businesses.EmptyState_Heading", "Δεν έχετε προσθέσει επιχειρήσεις ακόμα");
    public static string EmptyState_Description => Strings.Get("Businesses.EmptyState_Description", "Ξεκινήστε προσθέτοντας την πρώτη επιχείρηση που εκπροσωπείτε.");
    public static string Confirm_Deactivate_Message => Strings.Get("Businesses.Confirm_Deactivate_Message", "Σίγουρα θέλετε να απενεργοποιήσετε την επιχείρηση «{0}»; Δεν θα διαγραφούν δεδομένα — απλώς θα αποκρυφτεί από τη λίστα.");
    public static string Form_NameLabel => Strings.Get("Businesses.Form_NameLabel", "Όνομα επιχείρησης");
    public static string Form_NameRequired => Strings.Get("Businesses.Form_NameRequired", "Το όνομα είναι υποχρεωτικό");
    public static string Form_AfmRequired => Strings.Get("Businesses.Form_AfmRequired", "Το ΑΦΜ είναι υποχρεωτικό");
    public static string Form_AfmHelper => Strings.Get("Businesses.Form_AfmHelper", "9 ψηφία");
    public static string Form_KadLabel => Strings.Get("Businesses.Form_KadLabel", "ΚΑΔ (προαιρετικό)");
    public static string Form_KadHelper => Strings.Get("Businesses.Form_KadHelper", "Κωδικός αριθμός δραστηριότητας");
    public static string Form_AddressLabel => Strings.Get("Businesses.Form_AddressLabel", "Διεύθυνση (προαιρετικό)");
    public static string InvalidAfm_Short => Strings.Get("Businesses.InvalidAfm_Short", "Μη έγκυρο ΑΦΜ");
    public static string AlreadyActive_Error => Strings.Get("Businesses.AlreadyActive_Error", "Η επιχείρηση είναι ήδη ενεργή.");
    public static string OtherActiveDuplicate_Error => Strings.Get("Businesses.OtherActiveDuplicate_Error", "Άλλη ενεργή επιχείρηση χρησιμοποιεί ήδη αυτό το ΑΦΜ.");
    public static string NoLongerExists_Error => Strings.Get("Businesses.NoLongerExists_Error", "Η επιχείρηση δεν υπάρχει πλέον.");
    public static string ReactivatingMessage => Strings.Get("Businesses.ReactivatingMessage", "Επανενεργοποίηση...");
    public static string Dup_Heading => Strings.Get("Businesses.Dup_Heading", "Έχετε ήδη απενεργοποιημένη επιχείρηση με αυτό το ΑΦΜ");
    public static string Dup_PreviousNameLabel => Strings.Get("Businesses.Dup_PreviousNameLabel", "Όνομα της προηγούμενης επιχείρησης:");
    public static string Dup_AddressPrefix => Strings.Get("Businesses.Dup_AddressPrefix", "Διεύθυνση: {0}");
    public static string Dup_WhatToDo => Strings.Get("Businesses.Dup_WhatToDo", "Τι θέλετε να κάνετε;");
    public static string Dup_ReactivateOption => Strings.Get("Businesses.Dup_ReactivateOption", "Επανενεργοποίηση της προηγούμενης");
    public static string Dup_ReactivateExplain_History => Strings.Get("Businesses.Dup_ReactivateExplain_History", "Διατηρείται όλη η ιστορία της επιχείρησης (παραστατικά, τραπεζικοί λογαριασμοί κλπ).");
    public static string Dup_ReactivateExplain_Update => Strings.Get("Businesses.Dup_ReactivateExplain_Update", "Τα νέα στοιχεία που πληκτρολογήσατε θα ενημερώσουν την υπάρχουσα εγγραφή.");

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
        ["Businesses.List_Subtitle"] = "Manage the businesses you represent.",
        ["Businesses.List_CardHeading"] = "Business list",
        ["Businesses.EmptyState_Heading"] = "You have not added any businesses yet",
        ["Businesses.EmptyState_Description"] = "Start by adding the first business you represent.",
        ["Businesses.Confirm_Deactivate_Message"] = "Are you sure you want to deactivate the business “{0}”? No data is deleted — it is simply hidden from the list.",
        ["Businesses.Form_NameLabel"] = "Business name",
        ["Businesses.Form_NameRequired"] = "The name is required",
        ["Businesses.Form_AfmRequired"] = "The VAT number is required",
        ["Businesses.Form_AfmHelper"] = "9 digits",
        ["Businesses.Form_KadLabel"] = "Activity code (optional)",
        ["Businesses.Form_KadHelper"] = "Business activity code",
        ["Businesses.Form_AddressLabel"] = "Address (optional)",
        ["Businesses.InvalidAfm_Short"] = "Invalid VAT number",
        ["Businesses.AlreadyActive_Error"] = "The business is already active.",
        ["Businesses.OtherActiveDuplicate_Error"] = "Another active business is already using this VAT number.",
        ["Businesses.NoLongerExists_Error"] = "The business no longer exists.",
        ["Businesses.ReactivatingMessage"] = "Reactivating…",
        ["Businesses.Dup_Heading"] = "You already have a deactivated business with this VAT number",
        ["Businesses.Dup_PreviousNameLabel"] = "Previous business name:",
        ["Businesses.Dup_AddressPrefix"] = "Address: {0}",
        ["Businesses.Dup_WhatToDo"] = "What would you like to do?",
        ["Businesses.Dup_ReactivateOption"] = "Reactivate the previous one",
        ["Businesses.Dup_ReactivateExplain_History"] = "The whole history of the business is kept (invoices, bank accounts and so on).",
        ["Businesses.Dup_ReactivateExplain_Update"] = "The new details you entered will update the existing record.",
    });
}
