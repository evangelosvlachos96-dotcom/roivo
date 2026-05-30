namespace Roivo.Resources;

/// <summary>
/// User-facing strings for AADE myDATA integration UI.
/// Add new strings here when introducing new AADE-related UI states.
/// {N} placeholders are positional arguments for <c>string.Format</c>.
/// </summary>
public static class Aade
{
    public const string ConnectButton = "Σύνδεση με AADE";
    public const string DisconnectButton = "Αποσύνδεση";
    public const string SyncNowButton = "Συγχρονισμός τώρα";
    public const string CredentialsEntryHeader = "Διαχείριση σύνδεσης AADE";
    public const string UserIdLabel = "AADE User ID";
    public const string SubscriptionKeyLabel = "Subscription Key";

    public const string Status_NotConnected = "Δεν έχει συνδεθεί ακόμα";
    public const string Status_Connected = "Συνδεδεμένο";
    public const string LastSyncLabel = "Τελευταίος συγχρονισμός: {0}";
    public const string LastSyncNever = "Δεν έχει συγχρονιστεί ακόμα";

    // Error messages — one per ConnectAadeResult variant
    public const string Error_InvalidCredentials = "Λάθος στοιχεία AADE. Δοκιμάστε ξανά.";
    public const string Error_AfmMismatch_Generic_CanEditAfm = "Τα στοιχεία AADE αντιστοιχούν σε διαφορετικό ΑΦΜ. Διορθώστε το ΑΦΜ της επιχείρησης ή χρησιμοποιήστε τα σωστά credentials.";
    public const string Error_AfmMismatch_Generic_CannotEditAfm = "Τα στοιχεία AADE αντιστοιχούν σε διαφορετικό ΑΦΜ από αυτό της επιχείρησης ({0}). Παρακαλώ χρησιμοποιήστε τα σωστά credentials για αυτή την επιχείρηση.";
    public const string Error_AfmMismatch_WithAfm_CanEditAfm = "Τα στοιχεία AADE ανήκουν στην επιχείρηση με ΑΦΜ {0}, αλλά αυτή η επιχείρηση στο Roivo έχει ΑΦΜ {1}. Διορθώστε το ΑΦΜ της επιχείρησης ή χρησιμοποιήστε τα σωστά credentials.";
    public const string Error_AfmMismatch_WithAfm_CannotEditAfm = "Τα στοιχεία AADE ανήκουν στην επιχείρηση με ΑΦΜ {0}, αλλά αυτή η επιχείρηση στο Roivo έχει ΑΦΜ {1}. Παρακαλώ χρησιμοποιήστε τα σωστά credentials για αυτή την επιχείρηση.";
    public const string Error_RateLimited = "Πάρα πολλές προσπάθειες. Δοκιμάστε ξανά σε λίγα λεπτά.";
    public const string Error_AadeUnavailable = "Η υπηρεσία AADE δεν είναι διαθέσιμη αυτή τη στιγμή. Δοκιμάστε ξανά αργότερα.";

    public const string Confirm_Disconnect = "Είστε σίγουρος ότι θέλετε να αποσυνδέσετε την επιχείρηση από το AADE; Θα χρειαστεί να εισάγετε ξανά τα στοιχεία για να συνδεθείτε.";

    // Connection-failure banner (shown on BusinessAade and BusinessInvoices when
    // a persistent AADE failure is recorded on the Business).
    public const string ConnectionFailureBannerTitle = "Πρόβλημα σύνδεσης AADE";
    public const string ConnectionFailureBannerSince = "Πρόβλημα από: {0:dd MMM yyyy HH:mm}";
    public const string FailureReasonInvalidCredentials = "Τα διαπιστευτήρια AADE δεν είναι πλέον έγκυρα. Πρέπει να συνδεθείτε ξανά.";
    public const string FailureReasonAfmMismatch = "Τα διαπιστευτήρια AADE ανήκουν σε διαφορετικό ΑΦΜ. Πρέπει να ελέγξετε τη σύνδεση.";
    public const string FailureReasonGeneric = "Ο συγχρονισμός AADE απέτυχε. Επικοινωνήστε με την υποστήριξη αν το πρόβλημα παραμένει.";

    // 24-hour failure notification email (sent by AadeFailureNotificationJob).
    // Body placeholders: {0} business name, {1} AFM, {2} failure start (local),
    // {3} translated reason, {4} reconnect URL.
    public const string FailureEmailSubject = "Πρόβλημα σύνδεσης AADE - Roivo";
    public const string FailureEmailGreeting = "Γεια σας {0},";
    public const string FailureEmailBody = "Η σύνδεση AADE για την επιχείρηση \"{0}\" (ΑΦΜ {1}) δεν λειτουργεί από τις {2:dd/MM/yyyy HH:mm}.\n\nΛόγος: {3}\n\nΕίσοδος στο Roivo για να επανασυνδέσετε:\n{4}";
    public const string FailureEmailSignature = "Η ομάδα του Roivo";
}
