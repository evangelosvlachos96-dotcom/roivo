namespace Roivo.Resources;

/// <summary>
/// User-facing strings for the accountant workspace — the consolidated
/// dashboard, the alert list and the month-end report (M9).
/// </summary>
public static class Accountant
{
    // Navigation
    public const string Nav_Workspace = "Χώρος Λογιστή";
    public const string Nav_Dashboard = "Πελάτες";
    public const string Nav_Alerts = "Ειδοποιήσεις";
    public const string Nav_Reports = "Αναφορές";

    // Dashboard page
    public const string Title = "Πίνακας Λογιστή";
    public const string Subtitle = "Όλες οι επιχειρήσεις που διαχειρίζεσαι, με μια ματιά.";
    public const string NoBusinesses = "Δεν διαχειρίζεσαι ακόμη καμία επιχείρηση.";

    // Summary bar
    public const string Summary_TotalBusinesses = "Επιχειρήσεις";
    public const string Summary_AverageMatchRate = "Μέσο ποσοστό αντιστοίχισης";
    public const string Summary_CashflowWarnings = "Με προειδοποίηση ταμείου";
    public const string Summary_TaxDeadlines = "Προθεσμίες (30 ημέρες)";

    // Business table
    public const string Column_Business = "Επιχείρηση";
    public const string Column_Afm = "ΑΦΜ";
    public const string Column_Connections = "Συνδέσεις";
    public const string Column_MatchRate = "Αντιστοίχιση";
    public const string Column_CashflowHealth = "Ταμείο";
    public const string Column_Runway = "Αυτονομία";
    public const string Column_TaxDeadlines = "Προθεσμίες";
    public const string Column_LastSync = "Τελευταίος συγχρονισμός";
    public const string Column_Actions = "Ενέργειες";

    // Connection badges
    public const string Aade = "ΑΑΔΕ";
    public const string Banking = "Τράπεζα";
    public const string Connected = "Συνδεδεμένο";
    public const string NotConnected = "Μη συνδεδεμένο";
    public const string NeverSynced = "Ποτέ";

    // Cashflow health
    public const string Health_Green = "Υγιές";
    public const string Health_Yellow = "Προσοχή";
    public const string Health_Red = "Κρίσιμο";
    public const string Health_Unknown = "Άγνωστο";
    public const string Runway_Unlimited = "Χωρίς προβλεπόμενο έλλειμμα";
    public const string Runway_Days = "{0} ημέρες";
    public const string Runway_Unknown = "Χωρίς πρόβλεψη";

    // Quick links to the per-business pages
    public const string Link_Reconciliation = "Αντιστοίχιση";
    public const string Link_Cashflow = "Ταμειακές ροές";
    public const string Link_TaxCalendar = "Φορολογικό ημερολόγιο";

    // Alerts page
    public const string Alerts_Title = "Ειδοποιήσεις";
    public const string Alerts_Subtitle = "Ταξινομημένες κατά σοβαρότητα, σε όλες τις επιχειρήσεις.";
    public const string Alerts_None = "Καμία ειδοποίηση. Όλα εντάξει.";
    public const string Alerts_Count = "{0} ειδοποιήσεις";

    public const string Severity_Critical = "Κρίσιμη";
    public const string Severity_Warning = "Προειδοποίηση";
    public const string Severity_Info = "Πληροφορία";

    public const string Alert_NegativeBalancePredicted = "Προβλέπεται αρνητικό υπόλοιπο";
    public const string Alert_TaxDueSoon = "Φορολογική υποχρέωση εντός 7 ημερών";
    public const string Alert_BankingSyncFailed = "Αποτυχία συγχρονισμού τράπεζας";
    public const string Alert_AadeSyncFailed = "Αποτυχία συγχρονισμού ΑΑΔΕ";
    public const string Alert_LowReconciliationRate = "Χαμηλό ποσοστό αντιστοίχισης";

    public const string Column_Severity = "Σοβαρότητα";
    public const string Column_Alert = "Ειδοποίηση";
    public const string Column_Date = "Ημερομηνία";
    public const string Column_Amount = "Ποσό";
    public const string Column_Detail = "Λεπτομέρεια";

    // Reports page
    public const string Reports_Title = "Συγκεντρωτικές Αναφορές";
    public const string Reports_Subtitle = "Αντιστοίχιση, φορολογικές υποχρεώσεις και ταμείο σε όλους τους πελάτες.";
    public const string Reports_Period = "Περίοδος: {0} – {1}";
    public const string Reports_Reconciliation = "Μηνιαία αντιστοίχιση";
    public const string Reports_TaxCalendar = "Συγκεντρωτικό φορολογικό ημερολόγιο";
    public const string Reports_TaxCalendarHorizon = "Υποχρεώσεις έως {0}";
    public const string Reports_Cashflow = "Σύγκριση ταμειακών ροών";
    public const string Reports_Totals = "Σύνολα";
    public const string Reports_PreviousMonth = "Προηγούμενος μήνας";
    public const string Reports_NextMonth = "Επόμενος μήνας";
    public const string Reports_ExportCsv = "Εξαγωγή CSV";
    public const string Reports_NoTaxObligations = "Καμία φορολογική υποχρέωση στο διάστημα.";
    public const string Reports_NoCashflowData = "Δεν υπάρχουν αποθηκευμένες προβλέψεις.";

    // Report columns
    public const string Column_Invoices = "Παραστατικά";
    public const string Column_Transactions = "Κινήσεις";
    public const string Column_Confirmed = "Επιβεβαιωμένες";
    public const string Column_Pending = "Σε εκκρεμότητα";
    public const string Column_Unreconciled = "Ανοιχτά";
    public const string Column_ReconciledAmount = "Αντιστοιχισμένο ποσό";
    public const string Column_TaxType = "Είδος φόρου";
    public const string Column_Period = "Περίοδος";
    public const string Column_DueDate = "Λήξη";
    public const string Column_Status = "Κατάσταση";
    public const string Column_Balance30 = "Υπόλοιπο 30 ημ.";
    public const string Column_Balance60 = "Υπόλοιπο 60 ημ.";
    public const string Column_Balance90 = "Υπόλοιπο 90 ημ.";
    public const string Column_PredictedInflow = "Προβλεπόμενες εισροές";
    public const string Column_PredictedOutflow = "Προβλεπόμενες εκροές";

    public const string Status_Paid = "Πληρωμένο";
    public const string Status_Overdue = "Εκπρόθεσμο";
    public const string Status_Pending = "Σε εκκρεμότητα";

    // CSV sheet titles, written as a leading label row in the export
    public const string Csv_ReconciliationSection = "Μηνιαία αντιστοίχιση";
    public const string Csv_TaxSection = "Φορολογικές υποχρεώσεις";
    public const string Csv_CashflowSection = "Ταμειακές ροές";
}
