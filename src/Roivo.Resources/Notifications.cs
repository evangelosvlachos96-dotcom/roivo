namespace Roivo.Resources;

/// <summary>User-facing strings for email notifications and their settings (M10).</summary>
public static class Notifications
{
    // Brand chrome shared by every email template.
    public const string Wordmark = "roivo";
    public const string Tagline = "Cashflow Intelligence";
    public const string EmailFooter = "Λαμβάνεις αυτό το μήνυμα επειδή έχεις ενεργοποιήσει τις ειδοποιήσεις στο Roivo.";
    public const string EmailFooterManage = "Διαχείριση ειδοποιήσεων";
    public const string TestSubjectPrefix = "[Δοκιμή]";

    // Settings page
    public const string Title = "Ειδοποιήσεις";
    public const string Nav_Notifications = "Ειδοποιήσεις";
    public const string Subtitle = "Διάλεξε ποια email θέλεις να λαμβάνεις και πότε.";
    public const string SaveButton = "Αποθήκευση";
    public const string SendTestButton = "Αποστολή δοκιμαστικού";
    public const string Saved = "Οι ρυθμίσεις ειδοποιήσεων αποθηκεύτηκαν.";
    public const string TestSent = "Το δοκιμαστικό email στάλθηκε.";

    public const string DailyDigestLabel = "Ημερήσια σύνοψη";
    public const string DailyDigestHelp = "Μία σύνοψη κάθε πρωί για όλες τις επιχειρήσεις που διαχειρίζεσαι.";
    public const string TaxReminderLabel = "Υπενθυμίσεις φορολογικών υποχρεώσεων";
    public const string TaxReminderHelp = "Ειδοποίηση πριν από κάθε καταληκτική ημερομηνία πληρωμής.";
    public const string TaxReminderDaysBeforeLabel = "Ημέρες πριν την προθεσμία";
    public const string CashflowAlertLabel = "Ειδοποιήσεις ταμειακών ροών";
    public const string CashflowAlertHelp = "Ειδοποίηση όταν η πρόβλεψη δείχνει ότι το υπόλοιπο πέφτει κάτω από το όριο.";
    public const string CashflowAlertThresholdLabel = "Όριο υπολοίπου (€)";
    public const string SyncFailureAlertLabel = "Ειδοποιήσεις αποτυχίας συγχρονισμού";
    public const string SyncFailureAlertHelp = "Ειδοποίηση όταν η σύνδεση με το AADE ή την τράπεζα σταματήσει να λειτουργεί.";
    public const string WeeklyReconciliationLabel = "Εβδομαδιαία σύνοψη συμφωνιών";
    public const string WeeklyReconciliationHelp = "Ποσοστό αυτόματης συμφωνίας και τι έμεινε ασυμφώνητο.";

    // Error and result strings
    public const string Error_Forbidden = "Δεν έχεις δικαίωμα διαχείρισης ειδοποιήσεων για αυτή την επιχείρηση.";
    public const string Error_NotFound = "Η επιχείρηση δεν βρέθηκε.";
    public const string Error_InvalidLeadTime = "Οι ημέρες υπενθύμισης πρέπει να είναι από 1 έως {0}.";
    public const string Error_SendFailed = "Η αποστολή του email απέτυχε: {0}";
    public const string Error_NoEmail = "Δεν υπάρχει επιβεβαιωμένη διεύθυνση email για τον λογαριασμό σου.";

    // Notification kind display names
    public const string Kind_DailyDigest = "Ημερήσια σύνοψη";
    public const string Kind_TaxReminder = "Υπενθύμιση φόρου";
    public const string Kind_CashflowAlert = "Ειδοποίηση ταμειακών ροών";
    public const string Kind_SyncFailure = "Αποτυχία συγχρονισμού";
    public const string Kind_WeeklyReconciliation = "Εβδομαδιαία συμφωνία";

    // Daily digest email
    public const string Digest_Subject = "Η ημερήσια σύνοψη Roivo — {0}";
    public const string Digest_Heading = "Η ημερήσια σύνοψή σου";
    public const string Digest_Intro = "Σύνοψη για {0} επιχειρήσεις, {1}.";
    public const string Digest_IntroEmpty = "Δεν υπάρχει νέα κίνηση για τις επιχειρήσεις σου σήμερα.";
    public const string Digest_Cta = "Άνοιγμα πίνακα ελέγχου";
    public const string Digest_RowFormat = "{0} νέα παραστατικά · {1} νέες κινήσεις · {2} ασυμφώνητα";

    // Tax reminder email
    public const string Tax_Subject = "Υπενθύμιση: φορολογική υποχρέωση σε {0} ημέρες — {1}";
    public const string Tax_SubjectTomorrow = "Αύριο λήγει φορολογική υποχρέωση — {0}";
    public const string Tax_Heading = "Επερχόμενες φορολογικές υποχρεώσεις";
    public const string Tax_Intro = "Για την επιχείρηση {0} υπάρχουν {1} υποχρεώσεις που λήγουν σύντομα.";
    public const string Tax_RowFormat = "{0} — λήξη {1} — εκτίμηση {2}";
    public const string Tax_Cta = "Άνοιγμα φορολογικού ημερολογίου";
    public const string Tax_Footnote = "Τα ποσά είναι εκτιμήσεις από τα στοιχεία σου και μπορεί να διαφέρουν από το τελικό.";

    // Cashflow alert email
    public const string Cashflow_Subject = "Προειδοποίηση ταμειακών ροών — {0}";
    public const string Cashflow_Heading = "Το προβλεπόμενο υπόλοιπο πέφτει χαμηλά";
    public const string Cashflow_Intro = "Η πρόβλεψη για την επιχείρηση {0} δείχνει υπόλοιπο {1} στις {2}.";
    public const string Cashflow_RowThreshold = "Όριο ειδοποίησης";
    public const string Cashflow_RowLowestBalance = "Χαμηλότερο προβλεπόμενο υπόλοιπο";
    public const string Cashflow_RowLowestDate = "Ημερομηνία";
    public const string Cashflow_Cta = "Άνοιγμα πρόβλεψης";
    public const string Cashflow_Footnote = "Η πρόβλεψη βασίζεται στο ιστορικό των τραπεζικών σου κινήσεων.";

    // Sync failure email
    public const string Sync_Subject = "Ο συγχρονισμός απέτυχε — {0}";
    public const string Sync_Heading = "Μια σύνδεση σταμάτησε να λειτουργεί";
    public const string Sync_Intro = "Ο συγχρονισμός για την επιχείρηση {0} αποτυγχάνει. Τα δεδομένα σου δεν ενημερώνονται.";
    public const string Sync_RowConnection = "Σύνδεση";
    public const string Sync_RowSince = "Από";
    public const string Sync_RowReason = "Αιτία";
    public const string Sync_Cta = "Έλεγχος σύνδεσης";

    // Weekly reconciliation email
    public const string Reconciliation_Subject = "Εβδομαδιαία σύνοψη συμφωνιών — {0}";
    public const string Reconciliation_Heading = "Η εβδομάδα σου σε συμφωνίες";
    public const string Reconciliation_Intro = "Για την επιχείρηση {0}, από {1} έως {2}.";
    public const string Reconciliation_RowMatchRate = "Ποσοστό συμφωνίας";
    public const string Reconciliation_RowConfirmed = "Επιβεβαιωμένες συμφωνίες";
    public const string Reconciliation_RowPending = "Σε αναμονή";
    public const string Reconciliation_RowUnmatchedInvoices = "Ασυμφώνητα παραστατικά";
    public const string Reconciliation_RowUnmatchedTransactions = "Ασυμφώνητες κινήσεις";
    public const string Reconciliation_Cta = "Άνοιγμα συμφωνιών";

    // Sample values used by the test email so the layout is recognisable.
    public const string Sample_BusinessName = "Δείγμα Επιχείρησης";
    public const string Sample_Notice = "Αυτό είναι δοκιμαστικό μήνυμα με πλασματικά στοιχεία.";
}
