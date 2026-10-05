namespace Roivo.Resources;

/// <summary>
/// User-facing strings for the accountant workspace — the consolidated
/// dashboard, the alert list and the month-end report (M9).
/// </summary>
public static class Accountant
{
    // Navigation
    public static string Nav_Workspace => Strings.Get("Accountant.Nav_Workspace", "Χώρος Λογιστή");
    public static string Nav_Dashboard => Strings.Get("Accountant.Nav_Dashboard", "Πελάτες");
    public static string Nav_Alerts => Strings.Get("Accountant.Nav_Alerts", "Ειδοποιήσεις");
    public static string Nav_Reports => Strings.Get("Accountant.Nav_Reports", "Αναφορές");

    // Dashboard page
    public static string Title => Strings.Get("Accountant.Title", "Πίνακας Λογιστή");
    public static string Subtitle => Strings.Get("Accountant.Subtitle", "Όλες οι επιχειρήσεις που διαχειρίζεσαι, με μια ματιά.");
    public static string NoBusinesses => Strings.Get("Accountant.NoBusinesses", "Δεν διαχειρίζεσαι ακόμη καμία επιχείρηση.");

    // Summary bar
    public static string Summary_TotalBusinesses => Strings.Get("Accountant.Summary_TotalBusinesses", "Επιχειρήσεις");
    public static string Summary_AverageMatchRate => Strings.Get("Accountant.Summary_AverageMatchRate", "Μέσο ποσοστό αντιστοίχισης");
    public static string Summary_CashflowWarnings => Strings.Get("Accountant.Summary_CashflowWarnings", "Με προειδοποίηση ταμείου");
    public static string Summary_TaxDeadlines => Strings.Get("Accountant.Summary_TaxDeadlines", "Προθεσμίες (30 ημέρες)");

    // Business table
    public static string Column_Business => Strings.Get("Accountant.Column_Business", "Επιχείρηση");
    public static string Column_Afm => Strings.Get("Accountant.Column_Afm", "ΑΦΜ");
    public static string Column_Connections => Strings.Get("Accountant.Column_Connections", "Συνδέσεις");
    public static string Column_MatchRate => Strings.Get("Accountant.Column_MatchRate", "Αντιστοίχιση");
    public static string Column_CashflowHealth => Strings.Get("Accountant.Column_CashflowHealth", "Ταμείο");
    public static string Column_Runway => Strings.Get("Accountant.Column_Runway", "Αυτονομία");
    public static string Column_TaxDeadlines => Strings.Get("Accountant.Column_TaxDeadlines", "Προθεσμίες");
    public static string Column_LastSync => Strings.Get("Accountant.Column_LastSync", "Τελευταίος συγχρονισμός");
    public static string Column_Actions => Strings.Get("Accountant.Column_Actions", "Ενέργειες");

    // Connection badges
    public static string Aade => Strings.Get("Accountant.Aade", "ΑΑΔΕ");
    public static string Banking => Strings.Get("Accountant.Banking", "Τράπεζα");
    public static string Connected => Strings.Get("Accountant.Connected", "Συνδεδεμένο");
    public static string NotConnected => Strings.Get("Accountant.NotConnected", "Μη συνδεδεμένο");
    public static string NeverSynced => Strings.Get("Accountant.NeverSynced", "Ποτέ");

    // Cashflow health
    public static string Health_Green => Strings.Get("Accountant.Health_Green", "Υγιές");
    public static string Health_Yellow => Strings.Get("Accountant.Health_Yellow", "Προσοχή");
    public static string Health_Red => Strings.Get("Accountant.Health_Red", "Κρίσιμο");
    public static string Health_Unknown => Strings.Get("Accountant.Health_Unknown", "Άγνωστο");
    public static string Runway_Unlimited => Strings.Get("Accountant.Runway_Unlimited", "Χωρίς προβλεπόμενο έλλειμμα");
    public static string Runway_Days => Strings.Get("Accountant.Runway_Days", "{0} ημέρες");
    public static string Runway_Unknown => Strings.Get("Accountant.Runway_Unknown", "Χωρίς πρόβλεψη");

    // Quick links to the per-business pages
    public static string Link_Reconciliation => Strings.Get("Accountant.Link_Reconciliation", "Αντιστοίχιση");
    public static string Link_Cashflow => Strings.Get("Accountant.Link_Cashflow", "Ταμειακές ροές");
    public static string Link_TaxCalendar => Strings.Get("Accountant.Link_TaxCalendar", "Φορολογικό ημερολόγιο");

    // Alerts page
    public static string Alerts_Title => Strings.Get("Accountant.Alerts_Title", "Ειδοποιήσεις");
    public static string Alerts_Subtitle => Strings.Get("Accountant.Alerts_Subtitle", "Ταξινομημένες κατά σοβαρότητα, σε όλες τις επιχειρήσεις.");
    public static string Alerts_None => Strings.Get("Accountant.Alerts_None", "Καμία ειδοποίηση. Όλα εντάξει.");
    public static string Alerts_Count => Strings.Get("Accountant.Alerts_Count", "{0} ειδοποιήσεις");

    public static string Severity_Critical => Strings.Get("Accountant.Severity_Critical", "Κρίσιμη");
    public static string Severity_Warning => Strings.Get("Accountant.Severity_Warning", "Προειδοποίηση");
    public static string Severity_Info => Strings.Get("Accountant.Severity_Info", "Πληροφορία");

    public static string Alert_NegativeBalancePredicted => Strings.Get("Accountant.Alert_NegativeBalancePredicted", "Προβλέπεται αρνητικό υπόλοιπο");
    public static string Alert_TaxDueSoon => Strings.Get("Accountant.Alert_TaxDueSoon", "Φορολογική υποχρέωση εντός 7 ημερών");
    public static string Alert_BankingSyncFailed => Strings.Get("Accountant.Alert_BankingSyncFailed", "Αποτυχία συγχρονισμού τράπεζας");
    public static string Alert_AadeSyncFailed => Strings.Get("Accountant.Alert_AadeSyncFailed", "Αποτυχία συγχρονισμού ΑΑΔΕ");
    public static string Alert_LowReconciliationRate => Strings.Get("Accountant.Alert_LowReconciliationRate", "Χαμηλό ποσοστό αντιστοίχισης");

    public static string Column_Severity => Strings.Get("Accountant.Column_Severity", "Σοβαρότητα");
    public static string Column_Alert => Strings.Get("Accountant.Column_Alert", "Ειδοποίηση");
    public static string Column_Date => Strings.Get("Accountant.Column_Date", "Ημερομηνία");
    public static string Column_Amount => Strings.Get("Accountant.Column_Amount", "Ποσό");
    public static string Column_Detail => Strings.Get("Accountant.Column_Detail", "Λεπτομέρεια");

    // Reports page
    public static string Reports_Title => Strings.Get("Accountant.Reports_Title", "Συγκεντρωτικές Αναφορές");
    public static string Reports_Subtitle => Strings.Get("Accountant.Reports_Subtitle", "Αντιστοίχιση, φορολογικές υποχρεώσεις και ταμείο σε όλους τους πελάτες.");
    public static string Reports_Period => Strings.Get("Accountant.Reports_Period", "Περίοδος: {0} – {1}");
    public static string Reports_Reconciliation => Strings.Get("Accountant.Reports_Reconciliation", "Μηνιαία αντιστοίχιση");
    public static string Reports_TaxCalendar => Strings.Get("Accountant.Reports_TaxCalendar", "Συγκεντρωτικό φορολογικό ημερολόγιο");
    public static string Reports_TaxCalendarHorizon => Strings.Get("Accountant.Reports_TaxCalendarHorizon", "Υποχρεώσεις έως {0}");
    public static string Reports_Cashflow => Strings.Get("Accountant.Reports_Cashflow", "Σύγκριση ταμειακών ροών");
    public static string Reports_Totals => Strings.Get("Accountant.Reports_Totals", "Σύνολα");
    public static string Reports_PreviousMonth => Strings.Get("Accountant.Reports_PreviousMonth", "Προηγούμενος μήνας");
    public static string Reports_NextMonth => Strings.Get("Accountant.Reports_NextMonth", "Επόμενος μήνας");
    public static string Reports_ExportCsv => Strings.Get("Accountant.Reports_ExportCsv", "Εξαγωγή CSV");
    public static string Reports_NoTaxObligations => Strings.Get("Accountant.Reports_NoTaxObligations", "Καμία φορολογική υποχρέωση στο διάστημα.");
    public static string Reports_NoCashflowData => Strings.Get("Accountant.Reports_NoCashflowData", "Δεν υπάρχουν αποθηκευμένες προβλέψεις.");

    // Report columns
    public static string Column_Invoices => Strings.Get("Accountant.Column_Invoices", "Παραστατικά");
    public static string Column_Transactions => Strings.Get("Accountant.Column_Transactions", "Κινήσεις");
    public static string Column_Confirmed => Strings.Get("Accountant.Column_Confirmed", "Επιβεβαιωμένες");
    public static string Column_Pending => Strings.Get("Accountant.Column_Pending", "Σε εκκρεμότητα");
    public static string Column_Unreconciled => Strings.Get("Accountant.Column_Unreconciled", "Ανοιχτά");
    public static string Column_ReconciledAmount => Strings.Get("Accountant.Column_ReconciledAmount", "Αντιστοιχισμένο ποσό");
    public static string Column_TaxType => Strings.Get("Accountant.Column_TaxType", "Είδος φόρου");
    public static string Column_Period => Strings.Get("Accountant.Column_Period", "Περίοδος");
    public static string Column_DueDate => Strings.Get("Accountant.Column_DueDate", "Λήξη");
    public static string Column_Status => Strings.Get("Accountant.Column_Status", "Κατάσταση");
    public static string Column_Balance30 => Strings.Get("Accountant.Column_Balance30", "Υπόλοιπο 30 ημ.");
    public static string Column_Balance60 => Strings.Get("Accountant.Column_Balance60", "Υπόλοιπο 60 ημ.");
    public static string Column_Balance90 => Strings.Get("Accountant.Column_Balance90", "Υπόλοιπο 90 ημ.");
    public static string Column_PredictedInflow => Strings.Get("Accountant.Column_PredictedInflow", "Προβλεπόμενες εισροές");
    public static string Column_PredictedOutflow => Strings.Get("Accountant.Column_PredictedOutflow", "Προβλεπόμενες εκροές");

    public static string Status_Paid => Strings.Get("Accountant.Status_Paid", "Πληρωμένο");
    public static string Status_Overdue => Strings.Get("Accountant.Status_Overdue", "Εκπρόθεσμο");
    public static string Status_Pending => Strings.Get("Accountant.Status_Pending", "Σε εκκρεμότητα");

    // CSV sheet titles, written as a leading label row in the export
    public static string Csv_ReconciliationSection => Strings.Get("Accountant.Csv_ReconciliationSection", "Μηνιαία αντιστοίχιση");
    public static string Csv_TaxSection => Strings.Get("Accountant.Csv_TaxSection", "Φορολογικές υποχρεώσεις");
    public static string Csv_CashflowSection => Strings.Get("Accountant.Csv_CashflowSection", "Ταμειακές ροές");

    /// <summary>English overrides. Keys absent here fall back to Greek.</summary>
    static Accountant() => Strings.RegisterEnglish(new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Accountant.Nav_Workspace"] = "Accountant Workspace",
        ["Accountant.Nav_Dashboard"] = "Clients",
        ["Accountant.Nav_Alerts"] = "Alerts",
        ["Accountant.Nav_Reports"] = "Reports",
        ["Accountant.Title"] = "Accountant Dashboard",
        ["Accountant.Subtitle"] = "Every business you manage, at a glance.",
        ["Accountant.NoBusinesses"] = "You do not manage any businesses yet.",
        ["Accountant.Summary_TotalBusinesses"] = "Businesses",
        ["Accountant.Summary_AverageMatchRate"] = "Average match rate",
        ["Accountant.Summary_CashflowWarnings"] = "With cashflow warnings",
        ["Accountant.Summary_TaxDeadlines"] = "Deadlines (30 days)",
        ["Accountant.Column_Business"] = "Business",
        ["Accountant.Column_Afm"] = "VAT No.",
        ["Accountant.Column_Connections"] = "Connections",
        ["Accountant.Column_MatchRate"] = "Match rate",
        ["Accountant.Column_CashflowHealth"] = "Cashflow",
        ["Accountant.Column_Runway"] = "Runway",
        ["Accountant.Column_TaxDeadlines"] = "Deadlines",
        ["Accountant.Column_LastSync"] = "Last sync",
        ["Accountant.Column_Actions"] = "Actions",
        ["Accountant.Aade"] = "AADE",
        ["Accountant.Banking"] = "Bank",
        ["Accountant.Connected"] = "Connected",
        ["Accountant.NotConnected"] = "Not connected",
        ["Accountant.NeverSynced"] = "Never",
        ["Accountant.Health_Green"] = "Healthy",
        ["Accountant.Health_Yellow"] = "Caution",
        ["Accountant.Health_Red"] = "Critical",
        ["Accountant.Health_Unknown"] = "Unknown",
        ["Accountant.Runway_Unlimited"] = "No shortfall predicted",
        ["Accountant.Runway_Days"] = "{0} days",
        ["Accountant.Runway_Unknown"] = "No forecast",
        ["Accountant.Link_Reconciliation"] = "Reconciliation",
        ["Accountant.Link_Cashflow"] = "Cashflow",
        ["Accountant.Link_TaxCalendar"] = "Tax calendar",
        ["Accountant.Alerts_Title"] = "Alerts",
        ["Accountant.Alerts_Subtitle"] = "Sorted by severity, across all businesses.",
        ["Accountant.Alerts_None"] = "No alerts. All clear.",
        ["Accountant.Alerts_Count"] = "{0} alerts",
        ["Accountant.Severity_Critical"] = "Critical",
        ["Accountant.Severity_Warning"] = "Warning",
        ["Accountant.Severity_Info"] = "Info",
        ["Accountant.Alert_NegativeBalancePredicted"] = "Negative balance predicted",
        ["Accountant.Alert_TaxDueSoon"] = "Tax obligation due within 7 days",
        ["Accountant.Alert_BankingSyncFailed"] = "Bank sync failed",
        ["Accountant.Alert_AadeSyncFailed"] = "AADE sync failed",
        ["Accountant.Alert_LowReconciliationRate"] = "Low reconciliation rate",
        ["Accountant.Column_Severity"] = "Severity",
        ["Accountant.Column_Alert"] = "Alert",
        ["Accountant.Column_Date"] = "Date",
        ["Accountant.Column_Amount"] = "Amount",
        ["Accountant.Column_Detail"] = "Detail",
        ["Accountant.Reports_Title"] = "Consolidated Reports",
        ["Accountant.Reports_Subtitle"] = "Reconciliation, tax obligations and cashflow across every client.",
        ["Accountant.Reports_Period"] = "Period: {0} – {1}",
        ["Accountant.Reports_Reconciliation"] = "Monthly reconciliation",
        ["Accountant.Reports_TaxCalendar"] = "Consolidated tax calendar",
        ["Accountant.Reports_TaxCalendarHorizon"] = "Obligations due by {0}",
        ["Accountant.Reports_Cashflow"] = "Cashflow comparison",
        ["Accountant.Reports_Totals"] = "Totals",
        ["Accountant.Reports_PreviousMonth"] = "Previous month",
        ["Accountant.Reports_NextMonth"] = "Next month",
        ["Accountant.Reports_ExportCsv"] = "Export CSV",
        ["Accountant.Reports_NoTaxObligations"] = "No tax obligations in this period.",
        ["Accountant.Reports_NoCashflowData"] = "There are no stored forecasts.",
        ["Accountant.Column_Invoices"] = "Invoices",
        ["Accountant.Column_Transactions"] = "Transactions",
        ["Accountant.Column_Confirmed"] = "Confirmed",
        ["Accountant.Column_Pending"] = "Pending",
        ["Accountant.Column_Unreconciled"] = "Open",
        ["Accountant.Column_ReconciledAmount"] = "Reconciled amount",
        ["Accountant.Column_TaxType"] = "Tax type",
        ["Accountant.Column_Period"] = "Period",
        ["Accountant.Column_DueDate"] = "Due",
        ["Accountant.Column_Status"] = "Status",
        ["Accountant.Column_Balance30"] = "Balance 30d",
        ["Accountant.Column_Balance60"] = "Balance 60d",
        ["Accountant.Column_Balance90"] = "Balance 90d",
        ["Accountant.Column_PredictedInflow"] = "Predicted inflows",
        ["Accountant.Column_PredictedOutflow"] = "Predicted outflows",
        ["Accountant.Status_Paid"] = "Paid",
        ["Accountant.Status_Overdue"] = "Overdue",
        ["Accountant.Status_Pending"] = "Pending",
        ["Accountant.Csv_ReconciliationSection"] = "Monthly reconciliation",
        ["Accountant.Csv_TaxSection"] = "Tax obligations",
        ["Accountant.Csv_CashflowSection"] = "Cashflow",
    });
}
