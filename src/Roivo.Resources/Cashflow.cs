namespace Roivo.Resources;

/// <summary>User-facing strings for cashflow forecasting and the tax calendar (M7).</summary>
public static class Cashflow
{
    // Page and navigation
    public const string Title = "Πρόβλεψη Ταμειακών Ροών";
    public const string Nav_Cashflow = "Ταμειακές Ροές";
    public const string Nav_TaxCalendar = "Φορολογικό Ημερολόγιο";
    public const string Subtitle = "Πρόβλεψη 90 ημερών από το ιστορικό σου.";

    // Headline metrics
    public const string CurrentBalance = "Τρέχον Υπόλοιπο";
    public const string Inflows = "Εισροές";
    public const string Outflows = "Εκροές";
    public const string BurnRate = "Ρυθμός Κατανάλωσης";
    public const string DaysOfRunway = "Ημέρες Αυτονομίας";
    public const string Forecast30Day = "Πρόβλεψη 30 ημερών";
    public const string Forecast60Day = "Πρόβλεψη 60 ημερών";
    public const string Forecast90Day = "Πρόβλεψη 90 ημερών";
    public const string AverageDailyInflow = "Μέση ημερήσια εισροή";
    public const string AverageDailyOutflow = "Μέση ημερήσια εκροή";
    public const string PredictedBalance = "Προβλεπόμενο υπόλοιπο";
    public const string ConfidenceBand = "Εύρος βεβαιότητας";
    public const string UnlimitedRunway = "Χωρίς προβλεπόμενο έλλειμμα";

    // View toggles
    public const string ViewDaily = "Ημερήσια";
    public const string ViewWeekly = "Εβδομαδιαία";
    public const string ViewMonthly = "Μηνιαία";

    // Tax calendar
    public const string TaxCalendar = "Φορολογικό Ημερολόγιο";
    public const string Tax_Vat = "ΦΠΑ";
    public const string Tax_IncomeTax = "Φόρος Εισοδήματος";
    public const string Tax_WithholdingTax = "Παρακρατούμενος Φόρος";
    public const string Tax_SocialSecurity = "ΕΦΚΑ";
    public const string Tax_ProfessionalTax = "Τέλος Επιτηδεύματος";
    public const string Tax_TaxPrepayment = "Προκαταβολή Φόρου";

    public const string Status_Paid = "Πληρωμένο";
    public const string Status_Pending = "Εκκρεμεί";
    public const string Status_Overdue = "Ληξιπρόθεσμο";

    public const string MarkAsPaid = "Σήμανση ως πληρωμένο";
    public const string ActualAmount = "Πραγματικό ποσό";
    public const string EstimatedAmount = "Εκτιμώμενο ποσό";
    public const string DueDate = "Ημερομηνία λήξης";
    public const string Period = "Περίοδος";
    public const string QuarterFilter = "Τρίμηνο";

    // Alerts
    public const string Alerts = "Ειδοποιήσεις";
    public const string Severity_Warning = "Προειδοποίηση";
    public const string Severity_Critical = "Κρίσιμο";
    public const string Alert_LowBalance = "Χαμηλό Υπόλοιπο";
    public const string Alert_NegativeBalance = "Αρνητικό Υπόλοιπο";
    public const string Alert_TaxDue = "Φορολογική υποχρέωση";
    public const string Alert_LargeExpense = "Μεγάλη δαπάνη";
    public const string NoAlerts = "Καμία ειδοποίηση.";

    // Categories
    public const string Categories = "Κατηγορίες";
    public const string CategoryName = "Ονομασία";
    public const string CategoryType = "Τύπος";
    public const string Type_Income = "Έσοδο";
    public const string Type_Expense = "Έξοδο";
    public const string RecurringDay = "Ημέρα μήνα";
    public const string Amount = "Ποσό";
    public const string AddRecurringItem = "Προσθήκη επαναλαμβανόμενου";
    public const string NoCategories = "Δεν έχουν οριστεί κατηγορίες.";

    // Empty and result states
    /// <summary>Placeholder: the minimum number of days of history required.</summary>
    public const string InsufficientHistory =
        "Χρειάζονται τουλάχιστον {0} ημέρες τραπεζικού ιστορικού για πρόβλεψη.";

    public const string TaxMarkedPaid = "Η υποχρέωση σημάνθηκε ως πληρωμένη.";
    public const string CategoryAdded = "Η κατηγορία προστέθηκε.";
    public const string NoObligations = "Δεν υπάρχουν υποχρεώσεις στο διάστημα.";

    // Errors
    public const string Error_AlreadyPaid = "Η υποχρέωση έχει ήδη σημανθεί ως πληρωμένη.";
    public const string Error_InvalidAmount = "Το ποσό δεν μπορεί να είναι αρνητικό.";

    /// <summary>
    /// Shown next to every estimate. The figures are projections from invoice
    /// history, not a filed return.
    /// </summary>
    public const string EstimateDisclaimer =
        "Οι εκτιμήσεις βασίζονται στο ιστορικό τιμολογίων και δεν αποτελούν φορολογική δήλωση.";
}
