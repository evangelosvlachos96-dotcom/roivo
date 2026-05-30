namespace Roivo.Resources;

/// <summary>User-facing strings for business management UI.</summary>
public static class Businesses
{
    public const string PageTitle_List = "Επιχειρήσεις";
    public const string PageTitle_Create = "Δημιουργία επιχείρησης";
    public const string PageTitle_Edit = "Επεξεργασία επιχείρησης";

    public const string AddButton = "Προσθήκη επιχείρησης";
    public const string EditButton = "Επεξεργασία";
    public const string DeactivateButton = "Απενεργοποίηση";
    public const string ReactivateButton = "Επανενεργοποίηση";

    public const string NameLabel = "Όνομα";
    public const string AfmLabel = "ΑΦΜ";
    public const string KadLabel = "ΚΑΔ";
    public const string AddressLabel = "Διεύθυνση";
    public const string StatusLabel = "Κατάσταση";
    public const string TypeLabel = "Τύπος";

    public const string Status_Active = "Ενεργή";
    public const string Status_Inactive = "Ανενεργή";

    public const string Confirm_Deactivate = "Είστε σίγουρος ότι θέλετε να απενεργοποιήσετε την επιχείρηση \"{0}\";";
    public const string Confirm_Reactivate = "Επανενεργοποίηση της επιχείρησης \"{0}\";";

    public const string InactiveDuplicateDetected_Heading = "Υπάρχει ήδη ανενεργή επιχείρηση με αυτό το ΑΦΜ";
    public const string InactiveDuplicateDetected_Description = "Η επιχείρηση \"{0}\" με ΑΦΜ {1} είχε απενεργοποιηθεί στις {2}. Μπορείτε να την επανενεργοποιήσετε.";
    public const string InactiveDuplicateDetected_BackToForm = "Πίσω στο φόρμα";

    public const string ActiveDuplicate_Error = "Υπάρχει ήδη ενεργή επιχείρηση με αυτό το ΑΦΜ.";
    public const string InvalidAfm_Error = "Μη έγκυρο ΑΦΜ.";
    public const string Forbidden_Error = "Δεν έχετε τα απαραίτητα δικαιώματα για αυτή την ενέργεια.";
    public const string NotFound_Error = "Η επιχείρηση δεν βρέθηκε.";

    public const string AadeColumn_Header = "AADE";
    public const string ManageAadeConnection = "Διαχείριση σύνδεσης AADE";

    // Empty states
    public const string EmptyList_Accountant = "Δεν έχετε προσθέσει ακόμα επιχειρήσεις. Κάντε κλικ στο \"Προσθήκη επιχείρησης\" για να ξεκινήσετε.";
    public const string EmptyList_BusinessOwner = "Δεν έχετε προσθέσει ακόμα την επιχείρησή σας.";
}
