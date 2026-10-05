namespace Roivo.Resources;

/// <summary>
/// User-facing strings for the bank-connection (PSD2) UI.
/// Add new strings here when introducing new banking UI states.
/// {N} placeholders are positional arguments for <c>string.Format</c>.
/// </summary>
public static class Banking
{
    public static string PageTitle => Strings.Get("Banking.PageTitle", "Τραπεζική σύνδεση");
    public static string Loading => Strings.Get("Banking.Loading", "Φόρτωση...");

    public static string ConnectButton => Strings.Get("Banking.ConnectButton", "Σύνδεση Τράπεζας");
    public static string ConnectingMessage => Strings.Get("Banking.ConnectingMessage", "Σύνδεση...");
    public static string ConnectIntro => Strings.Get("Banking.ConnectIntro", "Συνδέστε τον τραπεζικό λογαριασμό της επιχείρησης για να λαμβάνουμε αυτόματα τις κινήσεις της. Θα μεταφερθείτε στην τράπεζά σας για έγκριση.");
    public static string BankPickerLabel => Strings.Get("Banking.BankPickerLabel", "Επιλέξτε τράπεζα");
    public static string BankPickerPlaceholder => Strings.Get("Banking.BankPickerPlaceholder", "Αναζήτηση τράπεζας");

    public static string Status_NotConnected => Strings.Get("Banking.Status_NotConnected", "Δεν έχει συνδεθεί ακόμα");
    public static string ConnectedMessage => Strings.Get("Banking.ConnectedMessage", "Συνδεδεμένη");
    public static string ConnectedBankLabel => Strings.Get("Banking.ConnectedBankLabel", "Τράπεζα");
    public static string LastSyncLabel => Strings.Get("Banking.LastSyncLabel", "Τελευταίος συγχρονισμός");
    public static string LastSyncNever => Strings.Get("Banking.LastSyncNever", "Δεν έχει συγχρονιστεί ακόμα");
    public static string ConsentExpiresLabel => Strings.Get("Banking.ConsentExpiresLabel", "Η έγκριση λήγει");
    public static string AccountsLabel => Strings.Get("Banking.AccountsLabel", "Λογαριασμοί");

    public static string SyncNowButton => Strings.Get("Banking.SyncNowButton", "Συγχρονισμός τώρα");
    public static string SyncingMessage => Strings.Get("Banking.SyncingMessage", "Συγχρονισμός...");
    public static string DisconnectButton => Strings.Get("Banking.DisconnectButton", "Αποσύνδεση");
    public static string ReconnectButton => Strings.Get("Banking.ReconnectButton", "Επανασύνδεση");

    // {0} new transactions, {1} updated
    public static string SyncSucceeded => Strings.Get("Banking.SyncSucceeded", "Ο συγχρονισμός ολοκληρώθηκε: {0} νέες κινήσεις, {1} ενημερωμένες.");
    public static string SyncFailedMessage => Strings.Get("Banking.SyncFailedMessage", "Ο συγχρονισμός απέτυχε");
    // {0} error count
    public static string SyncFailedCount => Strings.Get("Banking.SyncFailedCount", "Ο συγχρονισμός απέτυχε {0} φορές στη σειρά.");

    public static string Confirm_Disconnect_Title => Strings.Get("Banking.Confirm_Disconnect_Title", "Αποσύνδεση τράπεζας");
    public static string Confirm_Disconnect => Strings.Get("Banking.Confirm_Disconnect", "Σίγουρα θέλετε να αποσυνδέσετε αυτή την επιχείρηση από την τράπεζα; Οι αποθηκευμένες κινήσεις θα διαγραφούν και θα χρειαστεί νέα έγκριση για να επανασυνδεθείτε.");
    public static string Disconnected => Strings.Get("Banking.Disconnected", "Η τραπεζική σύνδεση αφαιρέθηκε.");
    public static string ConnectionConfirmed => Strings.Get("Banking.ConnectionConfirmed", "Η τράπεζα συνδέθηκε. Λογαριασμοί: {0}.");

    // Error messages — one per result variant the UI can receive
    public static string Error_BusinessNotFound => Strings.Get("Banking.Error_BusinessNotFound", "Η επιχείρηση δεν βρέθηκε.");
    public static string Error_AlreadyConnected => Strings.Get("Banking.Error_AlreadyConnected", "Η επιχείρηση είναι ήδη συνδεδεμένη με τράπεζα. Αποσυνδέστε την πρώτα.");
    public static string Error_UnknownProvider => Strings.Get("Banking.Error_UnknownProvider", "Η τράπεζα που επιλέξατε δεν είναι διαθέσιμη. Επιλέξτε άλλη.");
    public static string Error_InvalidCode => Strings.Get("Banking.Error_InvalidCode", "Η έγκριση δεν ολοκληρώθηκε ή έληξε. Δοκιμάστε ξανά από την αρχή.");
    public static string Error_NoAccountsGranted => Strings.Get("Banking.Error_NoAccountsGranted", "Δεν δόθηκε πρόσβαση σε κανέναν λογαριασμό. Δοκιμάστε ξανά και επιλέξτε τουλάχιστον έναν.");
    public static string Error_NotConnected => Strings.Get("Banking.Error_NotConnected", "Η τραπεζική σύνδεση δεν είναι ενεργή.");
    public static string Error_SessionExpired => Strings.Get("Banking.Error_SessionExpired", "Η έγκριση της τράπεζας έληξε. Χρειάζεται επανασύνδεση.");
    public static string Error_BankingUnavailable => Strings.Get("Banking.Error_BankingUnavailable", "Η υπηρεσία τραπεζικών δεδομένων δεν είναι διαθέσιμη αυτή τη στιγμή. Δοκιμάστε ξανά αργότερα.");
    public static string Error_NoProviders => Strings.Get("Banking.Error_NoProviders", "Δεν βρέθηκαν διαθέσιμες τράπεζες αυτή τη στιγμή.");

    // Connection-failure banner (shown on the banking page when a failure streak
    // is recorded on the Business).
    public static string ConnectionFailureBannerTitle => Strings.Get("Banking.ConnectionFailureBannerTitle", "Πρόβλημα τραπεζικής σύνδεσης");
    public static string ConnectionFailureBannerSince => Strings.Get("Banking.ConnectionFailureBannerSince", "Πρόβλημα από: {0:dd MMM yyyy HH:mm}");
    public static string FailureReasonSessionExpired => Strings.Get("Banking.FailureReasonSessionExpired", "Η έγκριση της τράπεζας έληξε ή ανακλήθηκε. Πρέπει να συνδεθείτε ξανά.");
    public static string FailureReasonUnauthorized => Strings.Get("Banking.FailureReasonUnauthorized", "Η τράπεζα απέρριψε την πρόσβαση. Πρέπει να συνδεθείτε ξανά.");
    public static string FailureReasonNetwork => Strings.Get("Banking.FailureReasonNetwork", "Δεν ήταν δυνατή η επικοινωνία με την τράπεζα. Συνήθως αποκαθίσταται μόνο του.");
    public static string FailureReasonGeneric => Strings.Get("Banking.FailureReasonGeneric", "Ο τραπεζικός συγχρονισμός απέτυχε. Επικοινωνήστε με την υποστήριξη αν το πρόβλημα παραμένει.");

    public static string ConnectionLabel => Strings.Get("Banking.ConnectionLabel", "Τραπεζική σύνδεση");

    /// <summary>English overrides. Keys absent here fall back to Greek.</summary>
    static Banking() => Strings.RegisterEnglish(new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Banking.PageTitle"] = "Bank connection",
        ["Banking.Loading"] = "Loading…",
        ["Banking.ConnectButton"] = "Connect bank",
        ["Banking.ConnectingMessage"] = "Connecting…",
        ["Banking.ConnectIntro"] = "Connect the business bank account so we receive its transactions automatically. You will be taken to your bank to approve.",
        ["Banking.BankPickerLabel"] = "Select a bank",
        ["Banking.BankPickerPlaceholder"] = "Search for a bank",
        ["Banking.Status_NotConnected"] = "Not connected yet",
        ["Banking.ConnectedMessage"] = "Connected",
        ["Banking.ConnectedBankLabel"] = "Bank",
        ["Banking.LastSyncLabel"] = "Last sync",
        ["Banking.LastSyncNever"] = "Never synced",
        ["Banking.ConsentExpiresLabel"] = "Consent expires",
        ["Banking.AccountsLabel"] = "Accounts",
        ["Banking.SyncNowButton"] = "Sync now",
        ["Banking.SyncingMessage"] = "Syncing…",
        ["Banking.DisconnectButton"] = "Disconnect",
        ["Banking.ReconnectButton"] = "Reconnect",
        ["Banking.SyncSucceeded"] = "Sync complete: {0} new transactions, {1} updated.",
        ["Banking.SyncFailedMessage"] = "Sync failed",
        ["Banking.SyncFailedCount"] = "Sync has failed {0} times in a row.",
        ["Banking.Confirm_Disconnect_Title"] = "Disconnect bank",
        ["Banking.Confirm_Disconnect"] = "Are you sure you want to disconnect this business from its bank? The stored transactions will be deleted and you will need a new approval to reconnect.",
        ["Banking.Disconnected"] = "The bank connection has been removed.",
        ["Banking.ConnectionConfirmed"] = "The bank is connected. Accounts: {0}.",
        ["Banking.Error_BusinessNotFound"] = "Business not found.",
        ["Banking.Error_AlreadyConnected"] = "This business is already connected to a bank. Disconnect it first.",
        ["Banking.Error_UnknownProvider"] = "The bank you selected is not available. Please choose another.",
        ["Banking.Error_InvalidCode"] = "The approval was not completed or has expired. Please start again from the beginning.",
        ["Banking.Error_NoAccountsGranted"] = "No account access was granted. Please try again and select at least one.",
        ["Banking.Error_NotConnected"] = "The bank connection is not active.",
        ["Banking.Error_SessionExpired"] = "The bank approval has expired. You need to reconnect.",
        ["Banking.Error_BankingUnavailable"] = "The banking data service is unavailable right now. Please try again later.",
        ["Banking.Error_NoProviders"] = "No banks are available right now.",
        ["Banking.ConnectionFailureBannerTitle"] = "Bank connection problem",
        ["Banking.ConnectionFailureBannerSince"] = "Problem since: {0:dd MMM yyyy HH:mm}",
        ["Banking.FailureReasonSessionExpired"] = "The bank approval has expired or was revoked. You need to connect again.",
        ["Banking.FailureReasonUnauthorized"] = "The bank refused access. You need to connect again.",
        ["Banking.FailureReasonNetwork"] = "We could not reach the bank. This usually resolves on its own.",
        ["Banking.FailureReasonGeneric"] = "Bank sync failed. Contact support if the problem persists.",
        ["Banking.ConnectionLabel"] = "Bank connection",
    });
}
