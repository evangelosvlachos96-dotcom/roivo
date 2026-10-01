namespace Roivo.Resources;

/// <summary>
/// User-facing strings for the bank-connection (PSD2) UI.
/// Add new strings here when introducing new banking UI states.
/// {N} placeholders are positional arguments for <c>string.Format</c>.
/// </summary>
public static class Banking
{
    public const string PageTitle = "Τραπεζική σύνδεση";
    public const string Loading = "Φόρτωση...";

    public const string ConnectButton = "Σύνδεση Τράπεζας";
    public const string ConnectingMessage = "Σύνδεση...";
    public const string ConnectIntro = "Συνδέστε τον τραπεζικό λογαριασμό της επιχείρησης για να λαμβάνουμε αυτόματα τις κινήσεις της. Θα μεταφερθείτε στην τράπεζά σας για έγκριση.";
    public const string BankPickerLabel = "Επιλέξτε τράπεζα";
    public const string BankPickerPlaceholder = "Αναζήτηση τράπεζας";

    public const string Status_NotConnected = "Δεν έχει συνδεθεί ακόμα";
    public const string ConnectedMessage = "Συνδεδεμένη";
    public const string ConnectedBankLabel = "Τράπεζα";
    public const string LastSyncLabel = "Τελευταίος συγχρονισμός";
    public const string LastSyncNever = "Δεν έχει συγχρονιστεί ακόμα";
    public const string ConsentExpiresLabel = "Η έγκριση λήγει";
    public const string AccountsLabel = "Λογαριασμοί";

    public const string SyncNowButton = "Συγχρονισμός τώρα";
    public const string SyncingMessage = "Συγχρονισμός...";
    public const string DisconnectButton = "Αποσύνδεση";
    public const string ReconnectButton = "Επανασύνδεση";

    // {0} new transactions, {1} updated
    public const string SyncSucceeded = "Ο συγχρονισμός ολοκληρώθηκε: {0} νέες κινήσεις, {1} ενημερωμένες.";
    public const string SyncFailedMessage = "Ο συγχρονισμός απέτυχε";
    // {0} error count
    public const string SyncFailedCount = "Ο συγχρονισμός απέτυχε {0} φορές στη σειρά.";

    public const string Confirm_Disconnect_Title = "Αποσύνδεση τράπεζας";
    public const string Confirm_Disconnect = "Σίγουρα θέλετε να αποσυνδέσετε αυτή την επιχείρηση από την τράπεζα; Οι αποθηκευμένες κινήσεις θα διαγραφούν και θα χρειαστεί νέα έγκριση για να επανασυνδεθείτε.";
    public const string Disconnected = "Η τραπεζική σύνδεση αφαιρέθηκε.";
    public const string ConnectionConfirmed = "Η τράπεζα συνδέθηκε. Λογαριασμοί: {0}.";

    // Error messages — one per result variant the UI can receive
    public const string Error_BusinessNotFound = "Η επιχείρηση δεν βρέθηκε.";
    public const string Error_AlreadyConnected = "Η επιχείρηση είναι ήδη συνδεδεμένη με τράπεζα. Αποσυνδέστε την πρώτα.";
    public const string Error_UnknownProvider = "Η τράπεζα που επιλέξατε δεν είναι διαθέσιμη. Επιλέξτε άλλη.";
    public const string Error_InvalidCode = "Η έγκριση δεν ολοκληρώθηκε ή έληξε. Δοκιμάστε ξανά από την αρχή.";
    public const string Error_NoAccountsGranted = "Δεν δόθηκε πρόσβαση σε κανέναν λογαριασμό. Δοκιμάστε ξανά και επιλέξτε τουλάχιστον έναν.";
    public const string Error_NotConnected = "Η τραπεζική σύνδεση δεν είναι ενεργή.";
    public const string Error_SessionExpired = "Η έγκριση της τράπεζας έληξε. Χρειάζεται επανασύνδεση.";
    public const string Error_BankingUnavailable = "Η υπηρεσία τραπεζικών δεδομένων δεν είναι διαθέσιμη αυτή τη στιγμή. Δοκιμάστε ξανά αργότερα.";
    public const string Error_NoProviders = "Δεν βρέθηκαν διαθέσιμες τράπεζες αυτή τη στιγμή.";

    // Connection-failure banner (shown on the banking page when a failure streak
    // is recorded on the Business).
    public const string ConnectionFailureBannerTitle = "Πρόβλημα τραπεζικής σύνδεσης";
    public const string ConnectionFailureBannerSince = "Πρόβλημα από: {0:dd MMM yyyy HH:mm}";
    public const string FailureReasonSessionExpired = "Η έγκριση της τράπεζας έληξε ή ανακλήθηκε. Πρέπει να συνδεθείτε ξανά.";
    public const string FailureReasonUnauthorized = "Η τράπεζα απέρριψε την πρόσβαση. Πρέπει να συνδεθείτε ξανά.";
    public const string FailureReasonNetwork = "Δεν ήταν δυνατή η επικοινωνία με την τράπεζα. Συνήθως αποκαθίσταται μόνο του.";
    public const string FailureReasonGeneric = "Ο τραπεζικός συγχρονισμός απέτυχε. Επικοινωνήστε με την υποστήριξη αν το πρόβλημα παραμένει.";

    // 24-hour failure notification email (sent by BankingFailureNotificationJob).
    // Body placeholders: {0} business name, {1} AFM, {2} failure start (local),
    // {3} consecutive failure count, {4} translated reason, {5} reconnect URL.
    public const string FailureEmailSubject = "Πρόβλημα τραπεζικής σύνδεσης - Roivo";
    public const string FailureEmailBody = "Η τραπεζική σύνδεση για την επιχείρηση \"{0}\" (ΑΦΜ {1}) δεν λειτουργεί από τις {2:dd/MM/yyyy HH:mm}.\n\nΑποτυχημένες προσπάθειες: {3}\nΛόγος: {4}\n\nΕίσοδος στο Roivo για να επανασυνδέσετε:\n{5}";
    public const string FailureEmailSignature = "Η ομάδα του Roivo";
}
