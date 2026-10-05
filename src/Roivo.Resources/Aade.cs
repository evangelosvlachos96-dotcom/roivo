namespace Roivo.Resources;

/// <summary>
/// User-facing strings for AADE myDATA integration UI.
/// Add new strings here when introducing new AADE-related UI states.
/// {N} placeholders are positional arguments for <c>string.Format</c>.
/// </summary>
public static class Aade
{
    public static string ConnectButton => Strings.Get("Aade.ConnectButton", "Σύνδεση με AADE");
    public static string DisconnectButton => Strings.Get("Aade.DisconnectButton", "Αποσύνδεση");
    public static string SyncNowButton => Strings.Get("Aade.SyncNowButton", "Συγχρονισμός τώρα");
    public static string CredentialsEntryHeader => Strings.Get("Aade.CredentialsEntryHeader", "Διαχείριση σύνδεσης AADE");
    public static string UserIdLabel => Strings.Get("Aade.UserIdLabel", "AADE User ID");
    public static string SubscriptionKeyLabel => Strings.Get("Aade.SubscriptionKeyLabel", "Subscription Key");

    public static string Status_NotConnected => Strings.Get("Aade.Status_NotConnected", "Δεν έχει συνδεθεί ακόμα");
    public static string Status_Connected => Strings.Get("Aade.Status_Connected", "Συνδεδεμένο");
    public static string LastSyncLabel => Strings.Get("Aade.LastSyncLabel", "Τελευταίος συγχρονισμός: {0}");
    public static string LastSyncNever => Strings.Get("Aade.LastSyncNever", "Δεν έχει συγχρονιστεί ακόμα");

    // Error messages — one per ConnectAadeResult variant
    public static string Error_InvalidCredentials => Strings.Get("Aade.Error_InvalidCredentials", "Λάθος στοιχεία AADE. Δοκιμάστε ξανά.");
    public static string Error_AfmMismatch_Generic_CanEditAfm => Strings.Get("Aade.Error_AfmMismatch_Generic_CanEditAfm", "Τα στοιχεία AADE αντιστοιχούν σε διαφορετικό ΑΦΜ. Διορθώστε το ΑΦΜ της επιχείρησης ή χρησιμοποιήστε τα σωστά credentials.");
    public static string Error_AfmMismatch_Generic_CannotEditAfm => Strings.Get("Aade.Error_AfmMismatch_Generic_CannotEditAfm", "Τα στοιχεία AADE αντιστοιχούν σε διαφορετικό ΑΦΜ από αυτό της επιχείρησης ({0}). Παρακαλώ χρησιμοποιήστε τα σωστά credentials για αυτή την επιχείρηση.");
    public static string Error_AfmMismatch_WithAfm_CanEditAfm => Strings.Get("Aade.Error_AfmMismatch_WithAfm_CanEditAfm", "Τα στοιχεία AADE ανήκουν στην επιχείρηση με ΑΦΜ {0}, αλλά αυτή η επιχείρηση στο Roivo έχει ΑΦΜ {1}. Διορθώστε το ΑΦΜ της επιχείρησης ή χρησιμοποιήστε τα σωστά credentials.");
    public static string Error_AfmMismatch_WithAfm_CannotEditAfm => Strings.Get("Aade.Error_AfmMismatch_WithAfm_CannotEditAfm", "Τα στοιχεία AADE ανήκουν στην επιχείρηση με ΑΦΜ {0}, αλλά αυτή η επιχείρηση στο Roivo έχει ΑΦΜ {1}. Παρακαλώ χρησιμοποιήστε τα σωστά credentials για αυτή την επιχείρηση.");
    public static string Error_RateLimited => Strings.Get("Aade.Error_RateLimited", "Πάρα πολλές προσπάθειες. Δοκιμάστε ξανά σε λίγα λεπτά.");
    public static string Error_AadeUnavailable => Strings.Get("Aade.Error_AadeUnavailable", "Η υπηρεσία AADE δεν είναι διαθέσιμη αυτή τη στιγμή. Δοκιμάστε ξανά αργότερα.");

    public static string Confirm_Disconnect => Strings.Get("Aade.Confirm_Disconnect", "Είστε σίγουρος ότι θέλετε να αποσυνδέσετε την επιχείρηση από το AADE; Θα χρειαστεί να εισάγετε ξανά τα στοιχεία για να συνδεθείτε.");

    // Connection-failure banner (shown on BusinessAade and BusinessInvoices when
    // a persistent AADE failure is recorded on the Business).
    public static string ConnectionFailureBannerTitle => Strings.Get("Aade.ConnectionFailureBannerTitle", "Πρόβλημα σύνδεσης AADE");
    public static string ConnectionFailureBannerSince => Strings.Get("Aade.ConnectionFailureBannerSince", "Πρόβλημα από: {0:dd MMM yyyy HH:mm}");
    public static string FailureReasonInvalidCredentials => Strings.Get("Aade.FailureReasonInvalidCredentials", "Τα διαπιστευτήρια AADE δεν είναι πλέον έγκυρα. Πρέπει να συνδεθείτε ξανά.");
    public static string FailureReasonAfmMismatch => Strings.Get("Aade.FailureReasonAfmMismatch", "Τα διαπιστευτήρια AADE ανήκουν σε διαφορετικό ΑΦΜ. Πρέπει να ελέγξετε τη σύνδεση.");
    public static string FailureReasonGeneric => Strings.Get("Aade.FailureReasonGeneric", "Ο συγχρονισμός AADE απέτυχε. Επικοινωνήστε με την υποστήριξη αν το πρόβλημα παραμένει.");

    public static string Connected_Description => Strings.Get("Aade.Connected_Description", "Συνδεδεμένη επιχείρηση με myDATA.");
    public static string LastSyncHeading => Strings.Get("Aade.LastSyncHeading", "Τελευταίος συγχρονισμός");
    public static string SyncingMessage => Strings.Get("Aade.SyncingMessage", "Συγχρονισμός...");
    public static string ConnectingMessage => Strings.Get("Aade.ConnectingMessage", "Σύνδεση...");
    public static string CredentialsIntro => Strings.Get("Aade.CredentialsIntro", "Εισαγάγετε τα AADE myDATA credentials σας. Αποθηκεύονται κρυπτογραφημένα στη βάση μας και χρησιμοποιούνται μόνο για τη λήψη παραστατικών.");
    public static string NotConnected_Description => Strings.Get("Aade.NotConnected_Description", "Συνδέστε αυτή την επιχείρηση με την AADE myDATA για να λαμβάνουμε αυτόματα τα παραστατικά της.");
    public static string NotConnected_Hint => Strings.Get("Aade.NotConnected_Hint", "Θα χρειαστείτε το AADE User ID και το Subscription Key που σας έχει χορηγηθεί από την AADE.");
    public static string ConnectSuccess => Strings.Get("Aade.ConnectSuccess", "Επιτυχής σύνδεση με AADE.");
    public static string Confirm_Disconnect_Title => Strings.Get("Aade.Confirm_Disconnect_Title", "Αποσύνδεση AADE");
    public static string Confirm_Disconnect_Message => Strings.Get("Aade.Confirm_Disconnect_Message", "Σίγουρα θέλετε να αποσυνδέσετε αυτή την επιχείρηση από την AADE myDATA; Δεν θα διαγραφούν τα ήδη ληφθέντα παραστατικά.");
    public static string Disconnected_Message => Strings.Get("Aade.Disconnected_Message", "Η σύνδεση αποσυνδέθηκε.");
    public static string SyncComplete => Strings.Get("Aade.SyncComplete", "Συγχρονισμός ολοκληρώθηκε: {0} νέα, {1} ενημερωμένα.");
    public static string Error_ConnectionInactive => Strings.Get("Aade.Error_ConnectionInactive", "Η σύνδεση AADE δεν είναι ενεργή.");
    public static string Error_CredentialsRejected => Strings.Get("Aade.Error_CredentialsRejected", "Τα AADE credentials απορρίφθηκαν. Συνδεθείτε ξανά.");
    public static string Error_Network => Strings.Get("Aade.Error_Network", "Σφάλμα δικτύου AADE: {0}");
    public static string Error_HttpStatus => Strings.Get("Aade.Error_HttpStatus", "Το AADE επέστρεψε {0}.");
    public static string Status_NotConnectedSentence => Strings.Get("Aade.Status_NotConnectedSentence", "Δεν έχει συνδεθεί ακόμα.");
    public static string Status_ConnectedWithLastSync => Strings.Get("Aade.Status_ConnectedWithLastSync", "Συνδεδεμένη. Τελευταίος συγχρονισμός: {0}");

    /// <summary>English overrides. Keys absent here fall back to Greek.</summary>
    static Aade() => Strings.RegisterEnglish(new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Aade.ConnectButton"] = "Connect to AADE",
        ["Aade.DisconnectButton"] = "Disconnect",
        ["Aade.SyncNowButton"] = "Sync now",
        ["Aade.CredentialsEntryHeader"] = "Manage AADE connection",
        ["Aade.UserIdLabel"] = "AADE User ID",
        ["Aade.SubscriptionKeyLabel"] = "Subscription Key",
        ["Aade.Status_NotConnected"] = "Not connected yet",
        ["Aade.Status_Connected"] = "Connected",
        ["Aade.LastSyncLabel"] = "Last sync: {0}",
        ["Aade.LastSyncNever"] = "Never synced",
        ["Aade.Error_InvalidCredentials"] = "Incorrect AADE credentials. Please try again.",
        ["Aade.Error_AfmMismatch_Generic_CanEditAfm"] = "These AADE credentials belong to a different VAT number. Correct the business VAT number or use the right credentials.",
        ["Aade.Error_AfmMismatch_Generic_CannotEditAfm"] = "These AADE credentials belong to a VAT number other than this business's ({0}). Please use the correct credentials for this business.",
        ["Aade.Error_AfmMismatch_WithAfm_CanEditAfm"] = "These AADE credentials belong to the business with VAT number {0}, but this business in Roivo has VAT number {1}. Correct the business VAT number or use the right credentials.",
        ["Aade.Error_AfmMismatch_WithAfm_CannotEditAfm"] = "These AADE credentials belong to the business with VAT number {0}, but this business in Roivo has VAT number {1}. Please use the correct credentials for this business.",
        ["Aade.Error_RateLimited"] = "Too many attempts. Please try again in a few minutes.",
        ["Aade.Error_AadeUnavailable"] = "The AADE service is unavailable right now. Please try again later.",
        ["Aade.Confirm_Disconnect"] = "Are you sure you want to disconnect this business from AADE? You will need to enter the credentials again to reconnect.",
        ["Aade.ConnectionFailureBannerTitle"] = "AADE connection problem",
        ["Aade.ConnectionFailureBannerSince"] = "Problem since: {0:dd MMM yyyy HH:mm}",
        ["Aade.FailureReasonInvalidCredentials"] = "The AADE credentials are no longer valid. You need to connect again.",
        ["Aade.FailureReasonAfmMismatch"] = "The AADE credentials belong to a different VAT number. Please check the connection.",
        ["Aade.FailureReasonGeneric"] = "AADE sync failed. Contact support if the problem persists.",
        ["Aade.Connected_Description"] = "This business is connected to myDATA.",
        ["Aade.LastSyncHeading"] = "Last sync",
        ["Aade.SyncingMessage"] = "Syncing…",
        ["Aade.ConnectingMessage"] = "Connecting…",
        ["Aade.CredentialsIntro"] = "Enter your AADE myDATA credentials. They are stored encrypted in our database and used only to fetch invoices.",
        ["Aade.NotConnected_Description"] = "Connect this business to AADE myDATA so that we fetch its invoices automatically.",
        ["Aade.NotConnected_Hint"] = "You will need the AADE User ID and Subscription Key issued to you by AADE.",
        ["Aade.ConnectSuccess"] = "Connected to AADE successfully.",
        ["Aade.Confirm_Disconnect_Title"] = "Disconnect AADE",
        ["Aade.Confirm_Disconnect_Message"] = "Are you sure you want to disconnect this business from AADE myDATA? Invoices already fetched are not deleted.",
        ["Aade.Disconnected_Message"] = "The connection was removed.",
        ["Aade.SyncComplete"] = "Sync complete: {0} new, {1} updated.",
        ["Aade.Error_ConnectionInactive"] = "The AADE connection is not active.",
        ["Aade.Error_CredentialsRejected"] = "The AADE credentials were rejected. Please connect again.",
        ["Aade.Error_Network"] = "AADE network error: {0}",
        ["Aade.Error_HttpStatus"] = "AADE returned {0}.",
        ["Aade.Status_NotConnectedSentence"] = "Not connected yet.",
        ["Aade.Status_ConnectedWithLastSync"] = "Connected. Last sync: {0}",
    });
}
