namespace Roivo.Resources;

/// <summary>User-facing strings for cashflow forecasting and the tax calendar (M7).</summary>
public static class Cashflow
{
    // Page and navigation
    public static string Title => Strings.Get("Cashflow.Title", "Πρόβλεψη Ταμειακών Ροών");
    public static string Nav_Cashflow => Strings.Get("Cashflow.Nav_Cashflow", "Ταμειακές Ροές");
    public static string Nav_TaxCalendar => Strings.Get("Cashflow.Nav_TaxCalendar", "Φορολογικό Ημερολόγιο");
    public static string Subtitle => Strings.Get("Cashflow.Subtitle", "Πρόβλεψη 90 ημερών από το ιστορικό σου.");

    // Headline metrics
    public static string CurrentBalance => Strings.Get("Cashflow.CurrentBalance", "Τρέχον Υπόλοιπο");
    public static string Inflows => Strings.Get("Cashflow.Inflows", "Εισροές");
    public static string Outflows => Strings.Get("Cashflow.Outflows", "Εκροές");
    public static string BurnRate => Strings.Get("Cashflow.BurnRate", "Ρυθμός Κατανάλωσης");
    public static string DaysOfRunway => Strings.Get("Cashflow.DaysOfRunway", "Ημέρες Αυτονομίας");
    public static string Forecast30Day => Strings.Get("Cashflow.Forecast30Day", "Πρόβλεψη 30 ημερών");
    public static string Forecast60Day => Strings.Get("Cashflow.Forecast60Day", "Πρόβλεψη 60 ημερών");
    public static string Forecast90Day => Strings.Get("Cashflow.Forecast90Day", "Πρόβλεψη 90 ημερών");
    public static string AverageDailyInflow => Strings.Get("Cashflow.AverageDailyInflow", "Μέση ημερήσια εισροή");
    public static string AverageDailyOutflow => Strings.Get("Cashflow.AverageDailyOutflow", "Μέση ημερήσια εκροή");
    public static string PredictedBalance => Strings.Get("Cashflow.PredictedBalance", "Προβλεπόμενο υπόλοιπο");
    public static string ConfidenceBand => Strings.Get("Cashflow.ConfidenceBand", "Εύρος βεβαιότητας");
    public static string UnlimitedRunway => Strings.Get("Cashflow.UnlimitedRunway", "Χωρίς προβλεπόμενο έλλειμμα");

    // View toggles
    public static string ViewDaily => Strings.Get("Cashflow.ViewDaily", "Ημερήσια");
    public static string ViewWeekly => Strings.Get("Cashflow.ViewWeekly", "Εβδομαδιαία");
    public static string ViewMonthly => Strings.Get("Cashflow.ViewMonthly", "Μηνιαία");

    // Tax calendar
    public static string TaxCalendar => Strings.Get("Cashflow.TaxCalendar", "Φορολογικό Ημερολόγιο");
    public static string Tax_Vat => Strings.Get("Cashflow.Tax_Vat", "ΦΠΑ");
    public static string Tax_IncomeTax => Strings.Get("Cashflow.Tax_IncomeTax", "Φόρος Εισοδήματος");
    public static string Tax_WithholdingTax => Strings.Get("Cashflow.Tax_WithholdingTax", "Παρακρατούμενος Φόρος");
    public static string Tax_SocialSecurity => Strings.Get("Cashflow.Tax_SocialSecurity", "ΕΦΚΑ");
    public static string Tax_ProfessionalTax => Strings.Get("Cashflow.Tax_ProfessionalTax", "Τέλος Επιτηδεύματος");
    public static string Tax_TaxPrepayment => Strings.Get("Cashflow.Tax_TaxPrepayment", "Προκαταβολή Φόρου");

    public static string Status_Paid => Strings.Get("Cashflow.Status_Paid", "Πληρωμένο");
    public static string Status_Pending => Strings.Get("Cashflow.Status_Pending", "Εκκρεμεί");
    public static string Status_Overdue => Strings.Get("Cashflow.Status_Overdue", "Ληξιπρόθεσμο");

    public static string MarkAsPaid => Strings.Get("Cashflow.MarkAsPaid", "Σήμανση ως πληρωμένο");
    public static string ActualAmount => Strings.Get("Cashflow.ActualAmount", "Πραγματικό ποσό");
    public static string EstimatedAmount => Strings.Get("Cashflow.EstimatedAmount", "Εκτιμώμενο ποσό");
    public static string DueDate => Strings.Get("Cashflow.DueDate", "Ημερομηνία λήξης");
    public static string Period => Strings.Get("Cashflow.Period", "Περίοδος");
    public static string QuarterFilter => Strings.Get("Cashflow.QuarterFilter", "Τρίμηνο");

    // Alerts
    public static string Alerts => Strings.Get("Cashflow.Alerts", "Ειδοποιήσεις");
    public static string Severity_Warning => Strings.Get("Cashflow.Severity_Warning", "Προειδοποίηση");
    public static string Severity_Critical => Strings.Get("Cashflow.Severity_Critical", "Κρίσιμο");
    public static string Alert_LowBalance => Strings.Get("Cashflow.Alert_LowBalance", "Χαμηλό Υπόλοιπο");
    public static string Alert_NegativeBalance => Strings.Get("Cashflow.Alert_NegativeBalance", "Αρνητικό Υπόλοιπο");
    public static string Alert_TaxDue => Strings.Get("Cashflow.Alert_TaxDue", "Φορολογική υποχρέωση");
    public static string Alert_LargeExpense => Strings.Get("Cashflow.Alert_LargeExpense", "Μεγάλη δαπάνη");
    public static string NoAlerts => Strings.Get("Cashflow.NoAlerts", "Καμία ειδοποίηση.");

    // Categories
    public static string Categories => Strings.Get("Cashflow.Categories", "Κατηγορίες");
    public static string CategoryName => Strings.Get("Cashflow.CategoryName", "Ονομασία");
    public static string CategoryType => Strings.Get("Cashflow.CategoryType", "Τύπος");
    public static string Type_Income => Strings.Get("Cashflow.Type_Income", "Έσοδο");
    public static string Type_Expense => Strings.Get("Cashflow.Type_Expense", "Έξοδο");
    public static string RecurringDay => Strings.Get("Cashflow.RecurringDay", "Ημέρα μήνα");
    public static string Amount => Strings.Get("Cashflow.Amount", "Ποσό");
    public static string AddRecurringItem => Strings.Get("Cashflow.AddRecurringItem", "Προσθήκη επαναλαμβανόμενου");
    public static string NoCategories => Strings.Get("Cashflow.NoCategories", "Δεν έχουν οριστεί κατηγορίες.");

    // Empty and result states
    /// <summary>Placeholder: the minimum number of days of history required.</summary>
    public static string InsufficientHistory => Strings.Get("Cashflow.InsufficientHistory", "Χρειάζονται τουλάχιστον {0} ημέρες τραπεζικού ιστορικού για πρόβλεψη.");

    public static string TaxMarkedPaid => Strings.Get("Cashflow.TaxMarkedPaid", "Η υποχρέωση σημάνθηκε ως πληρωμένη.");
    public static string CategoryAdded => Strings.Get("Cashflow.CategoryAdded", "Η κατηγορία προστέθηκε.");
    public static string NoObligations => Strings.Get("Cashflow.NoObligations", "Δεν υπάρχουν υποχρεώσεις στο διάστημα.");

    // Errors
    public static string Error_AlreadyPaid => Strings.Get("Cashflow.Error_AlreadyPaid", "Η υποχρέωση έχει ήδη σημανθεί ως πληρωμένη.");
    public static string Error_InvalidAmount => Strings.Get("Cashflow.Error_InvalidAmount", "Το ποσό δεν μπορεί να είναι αρνητικό.");

    /// <summary>
    /// Shown next to every estimate. The figures are projections from invoice
    /// history, not a filed return.
    /// </summary>
    public static string EstimateDisclaimer => Strings.Get("Cashflow.EstimateDisclaimer", "Οι εκτιμήσεις βασίζονται στο ιστορικό τιμολογίων και δεν αποτελούν φορολογική δήλωση.");

    /// <summary>English overrides. Keys absent here fall back to Greek.</summary>
    static Cashflow() => Strings.RegisterEnglish(new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Cashflow.Title"] = "Cashflow Forecast",
        ["Cashflow.Nav_Cashflow"] = "Cashflow",
        ["Cashflow.Nav_TaxCalendar"] = "Tax Calendar",
        ["Cashflow.Subtitle"] = "A 90-day forecast from your history.",
        ["Cashflow.CurrentBalance"] = "Current Balance",
        ["Cashflow.Inflows"] = "Inflows",
        ["Cashflow.Outflows"] = "Outflows",
        ["Cashflow.BurnRate"] = "Burn Rate",
        ["Cashflow.DaysOfRunway"] = "Days of Runway",
        ["Cashflow.Forecast30Day"] = "30-day forecast",
        ["Cashflow.Forecast60Day"] = "60-day forecast",
        ["Cashflow.Forecast90Day"] = "90-day forecast",
        ["Cashflow.AverageDailyInflow"] = "Average daily inflow",
        ["Cashflow.AverageDailyOutflow"] = "Average daily outflow",
        ["Cashflow.PredictedBalance"] = "Predicted balance",
        ["Cashflow.ConfidenceBand"] = "Confidence band",
        ["Cashflow.UnlimitedRunway"] = "No shortfall predicted",
        ["Cashflow.ViewDaily"] = "Daily",
        ["Cashflow.ViewWeekly"] = "Weekly",
        ["Cashflow.ViewMonthly"] = "Monthly",
        ["Cashflow.TaxCalendar"] = "Tax Calendar",
        ["Cashflow.Tax_Vat"] = "VAT",
        ["Cashflow.Tax_IncomeTax"] = "Income Tax",
        ["Cashflow.Tax_WithholdingTax"] = "Withholding Tax",
        ["Cashflow.Tax_SocialSecurity"] = "EFKA",
        ["Cashflow.Tax_ProfessionalTax"] = "Business Levy",
        ["Cashflow.Tax_TaxPrepayment"] = "Tax Prepayment",
        ["Cashflow.Status_Paid"] = "Paid",
        ["Cashflow.Status_Pending"] = "Pending",
        ["Cashflow.Status_Overdue"] = "Overdue",
        ["Cashflow.MarkAsPaid"] = "Mark as paid",
        ["Cashflow.ActualAmount"] = "Actual amount",
        ["Cashflow.EstimatedAmount"] = "Estimated amount",
        ["Cashflow.DueDate"] = "Due date",
        ["Cashflow.Period"] = "Period",
        ["Cashflow.QuarterFilter"] = "Quarter",
        ["Cashflow.Alerts"] = "Alerts",
        ["Cashflow.Severity_Warning"] = "Warning",
        ["Cashflow.Severity_Critical"] = "Critical",
        ["Cashflow.Alert_LowBalance"] = "Low Balance",
        ["Cashflow.Alert_NegativeBalance"] = "Negative Balance",
        ["Cashflow.Alert_TaxDue"] = "Tax due",
        ["Cashflow.Alert_LargeExpense"] = "Large expense",
        ["Cashflow.NoAlerts"] = "No alerts.",
        ["Cashflow.Categories"] = "Categories",
        ["Cashflow.CategoryName"] = "Name",
        ["Cashflow.CategoryType"] = "Type",
        ["Cashflow.Type_Income"] = "Income",
        ["Cashflow.Type_Expense"] = "Expense",
        ["Cashflow.RecurringDay"] = "Day of month",
        ["Cashflow.Amount"] = "Amount",
        ["Cashflow.AddRecurringItem"] = "Add recurring item",
        ["Cashflow.NoCategories"] = "No categories defined.",
        ["Cashflow.InsufficientHistory"] = "At least {0} days of bank history are needed for a forecast.",
        ["Cashflow.TaxMarkedPaid"] = "The obligation was marked as paid.",
        ["Cashflow.CategoryAdded"] = "The category was added.",
        ["Cashflow.NoObligations"] = "No obligations in this period.",
        ["Cashflow.Error_AlreadyPaid"] = "This obligation is already marked as paid.",
        ["Cashflow.Error_InvalidAmount"] = "The amount cannot be negative.",
        ["Cashflow.EstimateDisclaimer"] = "Estimates are based on invoice history and do not constitute a tax return.",
    });
}
