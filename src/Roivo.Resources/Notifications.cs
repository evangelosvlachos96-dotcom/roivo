namespace Roivo.Resources;

/// <summary>User-facing strings for email notifications and their settings (M10).</summary>
public static class Notifications
{
    // Brand chrome shared by every email template.
    public static string Wordmark => Strings.Get("Notifications.Wordmark", "roivo");
    public static string Tagline => Strings.Get("Notifications.Tagline", "Cashflow Intelligence");
    public static string EmailFooter => Strings.Get("Notifications.EmailFooter", "Λαμβάνεις αυτό το μήνυμα επειδή έχεις ενεργοποιήσει τις ειδοποιήσεις στο Roivo.");
    public static string EmailFooterManage => Strings.Get("Notifications.EmailFooterManage", "Διαχείριση ειδοποιήσεων");
    public static string TestSubjectPrefix => Strings.Get("Notifications.TestSubjectPrefix", "[Δοκιμή]");

    // Settings page
    public static string Title => Strings.Get("Notifications.Title", "Ειδοποιήσεις");
    public static string Nav_Notifications => Strings.Get("Notifications.Nav_Notifications", "Ειδοποιήσεις");
    public static string Subtitle => Strings.Get("Notifications.Subtitle", "Διάλεξε ποια email θέλεις να λαμβάνεις και πότε.");
    public static string SaveButton => Strings.Get("Notifications.SaveButton", "Αποθήκευση");
    public static string SendTestButton => Strings.Get("Notifications.SendTestButton", "Αποστολή δοκιμαστικού");
    public static string Saved => Strings.Get("Notifications.Saved", "Οι ρυθμίσεις ειδοποιήσεων αποθηκεύτηκαν.");
    public static string TestSent => Strings.Get("Notifications.TestSent", "Το δοκιμαστικό email στάλθηκε.");

    public static string DailyDigestLabel => Strings.Get("Notifications.DailyDigestLabel", "Ημερήσια σύνοψη");
    public static string DailyDigestHelp => Strings.Get("Notifications.DailyDigestHelp", "Μία σύνοψη κάθε πρωί για όλες τις επιχειρήσεις που διαχειρίζεσαι.");
    public static string TaxReminderLabel => Strings.Get("Notifications.TaxReminderLabel", "Υπενθυμίσεις φορολογικών υποχρεώσεων");
    public static string TaxReminderHelp => Strings.Get("Notifications.TaxReminderHelp", "Ειδοποίηση πριν από κάθε καταληκτική ημερομηνία πληρωμής.");
    public static string TaxReminderDaysBeforeLabel => Strings.Get("Notifications.TaxReminderDaysBeforeLabel", "Ημέρες πριν την προθεσμία");
    public static string CashflowAlertLabel => Strings.Get("Notifications.CashflowAlertLabel", "Ειδοποιήσεις ταμειακών ροών");
    public static string CashflowAlertHelp => Strings.Get("Notifications.CashflowAlertHelp", "Ειδοποίηση όταν η πρόβλεψη δείχνει ότι το υπόλοιπο πέφτει κάτω από το όριο.");
    public static string CashflowAlertThresholdLabel => Strings.Get("Notifications.CashflowAlertThresholdLabel", "Όριο υπολοίπου (€)");
    public static string SyncFailureAlertLabel => Strings.Get("Notifications.SyncFailureAlertLabel", "Ειδοποιήσεις αποτυχίας συγχρονισμού");
    public static string SyncFailureAlertHelp => Strings.Get("Notifications.SyncFailureAlertHelp", "Ειδοποίηση όταν η σύνδεση με το AADE ή την τράπεζα σταματήσει να λειτουργεί.");
    public static string WeeklyReconciliationLabel => Strings.Get("Notifications.WeeklyReconciliationLabel", "Εβδομαδιαία σύνοψη συμφωνιών");
    public static string WeeklyReconciliationHelp => Strings.Get("Notifications.WeeklyReconciliationHelp", "Ποσοστό αυτόματης συμφωνίας και τι έμεινε ασυμφώνητο.");

    // Error and result strings
    public static string Error_Forbidden => Strings.Get("Notifications.Error_Forbidden", "Δεν έχεις δικαίωμα διαχείρισης ειδοποιήσεων για αυτή την επιχείρηση.");
    public static string Error_NotFound => Strings.Get("Notifications.Error_NotFound", "Η επιχείρηση δεν βρέθηκε.");
    public static string Error_InvalidLeadTime => Strings.Get("Notifications.Error_InvalidLeadTime", "Οι ημέρες υπενθύμισης πρέπει να είναι από 1 έως {0}.");
    public static string Error_SendFailed => Strings.Get("Notifications.Error_SendFailed", "Η αποστολή του email απέτυχε: {0}");
    public static string Error_NoEmail => Strings.Get("Notifications.Error_NoEmail", "Δεν υπάρχει επιβεβαιωμένη διεύθυνση email για τον λογαριασμό σου.");

    // Notification kind display names
    public static string Kind_DailyDigest => Strings.Get("Notifications.Kind_DailyDigest", "Ημερήσια σύνοψη");
    public static string Kind_TaxReminder => Strings.Get("Notifications.Kind_TaxReminder", "Υπενθύμιση φόρου");
    public static string Kind_CashflowAlert => Strings.Get("Notifications.Kind_CashflowAlert", "Ειδοποίηση ταμειακών ροών");
    public static string Kind_SyncFailure => Strings.Get("Notifications.Kind_SyncFailure", "Αποτυχία συγχρονισμού");
    public static string Kind_WeeklyReconciliation => Strings.Get("Notifications.Kind_WeeklyReconciliation", "Εβδομαδιαία συμφωνία");

    // Daily digest email
    public static string Digest_Subject => Strings.Get("Notifications.Digest_Subject", "Η ημερήσια σύνοψη Roivo — {0}");
    public static string Digest_Heading => Strings.Get("Notifications.Digest_Heading", "Η ημερήσια σύνοψή σου");
    public static string Digest_Intro => Strings.Get("Notifications.Digest_Intro", "Σύνοψη για {0} επιχειρήσεις, {1}.");
    public static string Digest_IntroEmpty => Strings.Get("Notifications.Digest_IntroEmpty", "Δεν υπάρχει νέα κίνηση για τις επιχειρήσεις σου σήμερα.");
    public static string Digest_Cta => Strings.Get("Notifications.Digest_Cta", "Άνοιγμα πίνακα ελέγχου");
    public static string Digest_RowFormat => Strings.Get("Notifications.Digest_RowFormat", "{0} νέα παραστατικά · {1} νέες κινήσεις · {2} ασυμφώνητα");

    // Tax reminder email
    public static string Tax_Subject => Strings.Get("Notifications.Tax_Subject", "Υπενθύμιση: φορολογική υποχρέωση σε {0} ημέρες — {1}");
    public static string Tax_SubjectTomorrow => Strings.Get("Notifications.Tax_SubjectTomorrow", "Αύριο λήγει φορολογική υποχρέωση — {0}");
    public static string Tax_Heading => Strings.Get("Notifications.Tax_Heading", "Επερχόμενες φορολογικές υποχρεώσεις");
    public static string Tax_Intro => Strings.Get("Notifications.Tax_Intro", "Για την επιχείρηση {0} υπάρχουν {1} υποχρεώσεις που λήγουν σύντομα.");
    public static string Tax_RowFormat => Strings.Get("Notifications.Tax_RowFormat", "{0} — λήξη {1} — εκτίμηση {2}");
    public static string Tax_Cta => Strings.Get("Notifications.Tax_Cta", "Άνοιγμα φορολογικού ημερολογίου");
    public static string Tax_Footnote => Strings.Get("Notifications.Tax_Footnote", "Τα ποσά είναι εκτιμήσεις από τα στοιχεία σου και μπορεί να διαφέρουν από το τελικό.");

    // Cashflow alert email
    public static string Cashflow_Subject => Strings.Get("Notifications.Cashflow_Subject", "Προειδοποίηση ταμειακών ροών — {0}");
    public static string Cashflow_Heading => Strings.Get("Notifications.Cashflow_Heading", "Το προβλεπόμενο υπόλοιπο πέφτει χαμηλά");
    public static string Cashflow_Intro => Strings.Get("Notifications.Cashflow_Intro", "Η πρόβλεψη για την επιχείρηση {0} δείχνει υπόλοιπο {1} στις {2}.");
    public static string Cashflow_RowThreshold => Strings.Get("Notifications.Cashflow_RowThreshold", "Όριο ειδοποίησης");
    public static string Cashflow_RowLowestBalance => Strings.Get("Notifications.Cashflow_RowLowestBalance", "Χαμηλότερο προβλεπόμενο υπόλοιπο");
    public static string Cashflow_RowLowestDate => Strings.Get("Notifications.Cashflow_RowLowestDate", "Ημερομηνία");
    public static string Cashflow_Cta => Strings.Get("Notifications.Cashflow_Cta", "Άνοιγμα πρόβλεψης");
    public static string Cashflow_Footnote => Strings.Get("Notifications.Cashflow_Footnote", "Η πρόβλεψη βασίζεται στο ιστορικό των τραπεζικών σου κινήσεων.");

    // Sync failure email
    public static string Sync_Subject => Strings.Get("Notifications.Sync_Subject", "Ο συγχρονισμός απέτυχε — {0}");
    public static string Sync_Heading => Strings.Get("Notifications.Sync_Heading", "Μια σύνδεση σταμάτησε να λειτουργεί");
    public static string Sync_Intro => Strings.Get("Notifications.Sync_Intro", "Ο συγχρονισμός για την επιχείρηση {0} αποτυγχάνει. Τα δεδομένα σου δεν ενημερώνονται.");
    public static string Sync_RowConnection => Strings.Get("Notifications.Sync_RowConnection", "Σύνδεση");
    public static string Sync_RowSince => Strings.Get("Notifications.Sync_RowSince", "Από");
    public static string Sync_RowReason => Strings.Get("Notifications.Sync_RowReason", "Αιτία");
    public static string Sync_Cta => Strings.Get("Notifications.Sync_Cta", "Έλεγχος σύνδεσης");

    // Weekly reconciliation email
    public static string Reconciliation_Subject => Strings.Get("Notifications.Reconciliation_Subject", "Εβδομαδιαία σύνοψη συμφωνιών — {0}");
    public static string Reconciliation_Heading => Strings.Get("Notifications.Reconciliation_Heading", "Η εβδομάδα σου σε συμφωνίες");
    public static string Reconciliation_Intro => Strings.Get("Notifications.Reconciliation_Intro", "Για την επιχείρηση {0}, από {1} έως {2}.");
    public static string Reconciliation_RowMatchRate => Strings.Get("Notifications.Reconciliation_RowMatchRate", "Ποσοστό συμφωνίας");
    public static string Reconciliation_RowConfirmed => Strings.Get("Notifications.Reconciliation_RowConfirmed", "Επιβεβαιωμένες συμφωνίες");
    public static string Reconciliation_RowPending => Strings.Get("Notifications.Reconciliation_RowPending", "Σε αναμονή");
    public static string Reconciliation_RowUnmatchedInvoices => Strings.Get("Notifications.Reconciliation_RowUnmatchedInvoices", "Ασυμφώνητα παραστατικά");
    public static string Reconciliation_RowUnmatchedTransactions => Strings.Get("Notifications.Reconciliation_RowUnmatchedTransactions", "Ασυμφώνητες κινήσεις");
    public static string Reconciliation_Cta => Strings.Get("Notifications.Reconciliation_Cta", "Άνοιγμα συμφωνιών");

    // ΕΝΦΙΑ has no label in Cashflow.cs (the tax-calendar UI never renders it),
    // but the reminder email can name any obligation the calendar produced.
    public static string TaxLabel_PropertyTax => Strings.Get("Notifications.TaxLabel_PropertyTax", "ΕΝΦΙΑ");

    // Sample values used by the test email so the layout is recognisable.
    public static string Sample_BusinessName => Strings.Get("Notifications.Sample_BusinessName", "Δείγμα Επιχείρησης");
    public static string Sample_Notice => Strings.Get("Notifications.Sample_Notice", "Αυτό είναι δοκιμαστικό μήνυμα με πλασματικά στοιχεία.");

    /// <summary>English overrides. Keys absent here fall back to Greek.</summary>
    static Notifications() => Strings.RegisterEnglish(new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Notifications.Wordmark"] = "roivo",
        ["Notifications.Tagline"] = "Cashflow Intelligence",
        ["Notifications.EmailFooter"] = "You are receiving this message because you have notifications turned on in Roivo.",
        ["Notifications.EmailFooterManage"] = "Manage notifications",
        ["Notifications.TestSubjectPrefix"] = "[Test]",
        ["Notifications.Title"] = "Notifications",
        ["Notifications.Nav_Notifications"] = "Notifications",
        ["Notifications.Subtitle"] = "Choose which emails you receive and when.",
        ["Notifications.SaveButton"] = "Save",
        ["Notifications.SendTestButton"] = "Send test email",
        ["Notifications.Saved"] = "Your notification settings were saved.",
        ["Notifications.TestSent"] = "The test email was sent.",
        ["Notifications.DailyDigestLabel"] = "Daily digest",
        ["Notifications.DailyDigestHelp"] = "One digest each morning covering every business you manage.",
        ["Notifications.TaxReminderLabel"] = "Tax obligation reminders",
        ["Notifications.TaxReminderHelp"] = "A notice before each payment deadline.",
        ["Notifications.TaxReminderDaysBeforeLabel"] = "Days before the deadline",
        ["Notifications.CashflowAlertLabel"] = "Cashflow alerts",
        ["Notifications.CashflowAlertHelp"] = "A notice when the forecast shows your balance dropping below the threshold.",
        ["Notifications.CashflowAlertThresholdLabel"] = "Balance threshold (€)",
        ["Notifications.SyncFailureAlertLabel"] = "Sync failure alerts",
        ["Notifications.SyncFailureAlertHelp"] = "A notice when the AADE or bank connection stops working.",
        ["Notifications.WeeklyReconciliationLabel"] = "Weekly reconciliation digest",
        ["Notifications.WeeklyReconciliationHelp"] = "Automatic match rate and what was left unmatched.",
        ["Notifications.Error_Forbidden"] = "You do not have permission to manage notifications for this business.",
        ["Notifications.Error_NotFound"] = "Business not found.",
        ["Notifications.Error_InvalidLeadTime"] = "The reminder lead time must be between 1 and {0} days.",
        ["Notifications.Error_SendFailed"] = "Sending the email failed: {0}",
        ["Notifications.Error_NoEmail"] = "There is no confirmed email address on your account.",
        ["Notifications.Kind_DailyDigest"] = "Daily digest",
        ["Notifications.Kind_TaxReminder"] = "Tax reminder",
        ["Notifications.Kind_CashflowAlert"] = "Cashflow alert",
        ["Notifications.Kind_SyncFailure"] = "Sync failure",
        ["Notifications.Kind_WeeklyReconciliation"] = "Weekly reconciliation",
        ["Notifications.Digest_Subject"] = "Your Roivo daily digest — {0}",
        ["Notifications.Digest_Heading"] = "Your daily digest",
        ["Notifications.Digest_Intro"] = "A digest for {0} businesses, {1}.",
        ["Notifications.Digest_IntroEmpty"] = "There is no new activity for your businesses today.",
        ["Notifications.Digest_Cta"] = "Open dashboard",
        ["Notifications.Digest_RowFormat"] = "{0} new invoices · {1} new transactions · {2} unmatched",
        ["Notifications.Tax_Subject"] = "Reminder: tax obligation due in {0} days — {1}",
        ["Notifications.Tax_SubjectTomorrow"] = "A tax obligation is due tomorrow — {0}",
        ["Notifications.Tax_Heading"] = "Upcoming tax obligations",
        ["Notifications.Tax_Intro"] = "{0} has {1} obligations due soon.",
        ["Notifications.Tax_RowFormat"] = "{0} — due {1} — estimate {2}",
        ["Notifications.Tax_Cta"] = "Open tax calendar",
        ["Notifications.Tax_Footnote"] = "The amounts are estimates from your own data and may differ from the final figure.",
        ["Notifications.Cashflow_Subject"] = "Cashflow warning — {0}",
        ["Notifications.Cashflow_Heading"] = "Your predicted balance is running low",
        ["Notifications.Cashflow_Intro"] = "The forecast for {0} shows a balance of {1} on {2}.",
        ["Notifications.Cashflow_RowThreshold"] = "Alert threshold",
        ["Notifications.Cashflow_RowLowestBalance"] = "Lowest predicted balance",
        ["Notifications.Cashflow_RowLowestDate"] = "Date",
        ["Notifications.Cashflow_Cta"] = "Open forecast",
        ["Notifications.Cashflow_Footnote"] = "The forecast is based on the history of your bank transactions.",
        ["Notifications.Sync_Subject"] = "Sync failed — {0}",
        ["Notifications.Sync_Heading"] = "A connection has stopped working",
        ["Notifications.Sync_Intro"] = "Sync for {0} is failing. Your data is not being updated.",
        ["Notifications.Sync_RowConnection"] = "Connection",
        ["Notifications.Sync_RowSince"] = "Since",
        ["Notifications.Sync_RowReason"] = "Reason",
        ["Notifications.Sync_Cta"] = "Check connection",
        ["Notifications.Reconciliation_Subject"] = "Weekly reconciliation digest — {0}",
        ["Notifications.Reconciliation_Heading"] = "Your week in reconciliation",
        ["Notifications.Reconciliation_Intro"] = "For {0}, from {1} to {2}.",
        ["Notifications.Reconciliation_RowMatchRate"] = "Match rate",
        ["Notifications.Reconciliation_RowConfirmed"] = "Confirmed matches",
        ["Notifications.Reconciliation_RowPending"] = "Pending",
        ["Notifications.Reconciliation_RowUnmatchedInvoices"] = "Unmatched invoices",
        ["Notifications.Reconciliation_RowUnmatchedTransactions"] = "Unmatched transactions",
        ["Notifications.Reconciliation_Cta"] = "Open reconciliation",
        ["Notifications.TaxLabel_PropertyTax"] = "ENFIA (property tax)",
        ["Notifications.Sample_BusinessName"] = "Sample Business",
        ["Notifications.Sample_Notice"] = "This is a test message with fictitious data.",
    });
}
