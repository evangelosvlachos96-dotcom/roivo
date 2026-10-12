namespace Roivo.Resources;

/// <summary>User-facing strings for the reconciliation (M6) UI.</summary>
public static class Reconciliation
{
    // Page and navigation
    public static string Title => Strings.Get("Reconciliation.Title", "Αντιστοίχιση");
    public static string Nav_Reconciliation => Strings.Get("Reconciliation.Nav_Reconciliation", "Αντιστοίχιση");
    public static string Subtitle => Strings.Get("Reconciliation.Subtitle", "Σύνδεση τιμολογίων με τραπεζικές συναλλαγές.");

    // Match kinds
    public static string AutomaticMatching => Strings.Get("Reconciliation.AutomaticMatching", "Αυτόματη αντιστοίχιση");
    public static string ManualMatching => Strings.Get("Reconciliation.ManualMatching", "Χειροκίνητη αντιστοίχιση");
    public static string SuggestedMatches => Strings.Get("Reconciliation.SuggestedMatches", "Προτεινόμενες αντιστοιχίσεις");

    // Unmatched
    public static string UnmatchedInvoices => Strings.Get("Reconciliation.UnmatchedInvoices", "Μη αντιστοιχισμένα τιμολόγια");
    public static string UnmatchedTransactions => Strings.Get("Reconciliation.UnmatchedTransactions", "Μη αντιστοιχισμένες συναλλαγές");

    // Metrics
    public static string MatchRate => Strings.Get("Reconciliation.MatchRate", "Ποσοστό αντιστοίχισης");
    public static string ReconciledAmount => Strings.Get("Reconciliation.ReconciledAmount", "Αντιστοιχισμένο ποσό");
    public static string TotalInvoices => Strings.Get("Reconciliation.TotalInvoices", "Σύνολο τιμολογίων");
    public static string TotalTransactions => Strings.Get("Reconciliation.TotalTransactions", "Σύνολο συναλλαγών");
    public static string ConfirmedMatches => Strings.Get("Reconciliation.ConfirmedMatches", "Επιβεβαιωμένες αντιστοιχίσεις");
    public static string PendingMatches => Strings.Get("Reconciliation.PendingMatches", "Εκκρεμείς αντιστοιχίσεις");
    public static string Confidence => Strings.Get("Reconciliation.Confidence", "Βεβαιότητα");

    // Actions
    public static string RunReconciliation => Strings.Get("Reconciliation.RunReconciliation", "Εκτέλεση αντιστοίχισης");
    public static string Confirm => Strings.Get("Reconciliation.Confirm", "Επιβεβαίωση");
    public static string Reject => Strings.Get("Reconciliation.Reject", "Απόρριψη");
    public static string MatchSelected => Strings.Get("Reconciliation.MatchSelected", "Αντιστοίχιση επιλεγμένων");
    public static string SuggestMatches => Strings.Get("Reconciliation.SuggestMatches", "Πρόταση αντιστοιχίσεων");
    public static string ExportCsv => Strings.Get("Reconciliation.ExportCsv", "Εξαγωγή σε CSV");

    // History
    public static string RecentMatches => Strings.Get("Reconciliation.RecentMatches", "Πρόσφατες αντιστοιχίσεις");
    public static string MatchedVsUnmatched => Strings.Get("Reconciliation.MatchedVsUnmatched", "Αντιστοιχισμένα / μη αντιστοιχισμένα");
    public static string History => Strings.Get("Reconciliation.History", "Ιστορικό αντιστοιχίσεων");
    public static string FilterByStatus => Strings.Get("Reconciliation.FilterByStatus", "Φίλτρο κατάστασης");
    public static string FilterAll => Strings.Get("Reconciliation.FilterAll", "Όλες");

    // Table headers
    public static string Column_Date => Strings.Get("Reconciliation.Column_Date", "Ημερομηνία");
    public static string Column_Amount => Strings.Get("Reconciliation.Column_Amount", "Ποσό");
    public static string Column_Counterparty => Strings.Get("Reconciliation.Column_Counterparty", "Αντισυμβαλλόμενος");
    public static string Column_Description => Strings.Get("Reconciliation.Column_Description", "Περιγραφή");
    public static string Column_Type => Strings.Get("Reconciliation.Column_Type", "Τύπος");
    public static string Column_Status => Strings.Get("Reconciliation.Column_Status", "Κατάσταση");
    public static string Column_Invoice => Strings.Get("Reconciliation.Column_Invoice", "Τιμολόγιο");
    public static string Column_Transaction => Strings.Get("Reconciliation.Column_Transaction", "Συναλλαγή");

    // Statuses
    public static string Status_Confirmed => Strings.Get("Reconciliation.Status_Confirmed", "Επιβεβαιωμένη");
    public static string Status_Pending => Strings.Get("Reconciliation.Status_Pending", "Εκκρεμής");
    public static string Status_Rejected => Strings.Get("Reconciliation.Status_Rejected", "Απορριφθείσα");
    public static string Type_Automatic => Strings.Get("Reconciliation.Type_Automatic", "Αυτόματη");
    public static string Type_Manual => Strings.Get("Reconciliation.Type_Manual", "Χειροκίνητη");
    public static string Type_Suggested => Strings.Get("Reconciliation.Type_Suggested", "Προτεινόμενη");

    // Date range
    public static string DateRange => Strings.Get("Reconciliation.DateRange", "Χρονικό διάστημα");
    public static string DateFrom => Strings.Get("Reconciliation.DateFrom", "Από");
    public static string DateTo => Strings.Get("Reconciliation.DateTo", "Έως");

    // Empty and result states
    public static string NoUnmatchedInvoices => Strings.Get("Reconciliation.NoUnmatchedInvoices", "Δεν υπάρχουν μη αντιστοιχισμένα τιμολόγια.");
    public static string NoUnmatchedTransactions => Strings.Get("Reconciliation.NoUnmatchedTransactions", "Δεν υπάρχουν μη αντιστοιχισμένες συναλλαγές.");
    public static string NoMatches => Strings.Get("Reconciliation.NoMatches", "Δεν υπάρχουν αντιστοιχίσεις ακόμη.");
    public static string SelectBothSides => Strings.Get("Reconciliation.SelectBothSides", "Επιλέξτε ένα τιμολόγιο και μία συναλλαγή.");

    /// <summary>Placeholders: automatic count, suggested count.</summary>
    public static string RunCompleted => Strings.Get("Reconciliation.RunCompleted", "Ολοκληρώθηκε: {0} αυτόματες, {1} προτεινόμενες.");

    public static string MatchConfirmed => Strings.Get("Reconciliation.MatchConfirmed", "Η αντιστοίχιση επιβεβαιώθηκε.");
    public static string MatchRejected => Strings.Get("Reconciliation.MatchRejected", "Η αντιστοίχιση απορρίφθηκε.");
    public static string MatchCreated => Strings.Get("Reconciliation.MatchCreated", "Η αντιστοίχιση δημιουργήθηκε.");

    // Errors
    public static string Error_AlreadyMatched => Strings.Get("Reconciliation.Error_AlreadyMatched", "Αυτό το ζεύγος έχει ήδη αντιστοιχιστεί.");
    public static string Error_SideAlreadyReconciled => Strings.Get("Reconciliation.Error_SideAlreadyReconciled", "Το τιμολόγιο ή η συναλλαγή έχει ήδη αντιστοιχιστεί με κάτι άλλο.");
    public static string Error_NotPending => Strings.Get("Reconciliation.Error_NotPending", "Η αντιστοίχιση δεν εκκρεμεί πλέον.");
    public static string Error_InvalidDateRange => Strings.Get("Reconciliation.Error_InvalidDateRange", "Η ημερομηνία «Έως» πρέπει να είναι μετά την «Από».");

    /// <summary>English overrides. Keys absent here fall back to Greek.</summary>
    static Reconciliation() => Strings.RegisterEnglish(new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Reconciliation.Title"] = "Reconciliation",
        ["Reconciliation.Nav_Reconciliation"] = "Reconciliation",
        ["Reconciliation.Subtitle"] = "Linking invoices to bank transactions.",
        ["Reconciliation.AutomaticMatching"] = "Automatic matching",
        ["Reconciliation.ManualMatching"] = "Manual matching",
        ["Reconciliation.SuggestedMatches"] = "Suggested matches",
        ["Reconciliation.UnmatchedInvoices"] = "Unmatched invoices",
        ["Reconciliation.UnmatchedTransactions"] = "Unmatched transactions",
        ["Reconciliation.MatchRate"] = "Match rate",
        ["Reconciliation.ReconciledAmount"] = "Reconciled amount",
        ["Reconciliation.TotalInvoices"] = "Total invoices",
        ["Reconciliation.TotalTransactions"] = "Total transactions",
        ["Reconciliation.ConfirmedMatches"] = "Confirmed matches",
        ["Reconciliation.PendingMatches"] = "Pending matches",
        ["Reconciliation.Confidence"] = "Confidence",
        ["Reconciliation.RunReconciliation"] = "Run reconciliation",
        ["Reconciliation.Confirm"] = "Confirm",
        ["Reconciliation.Reject"] = "Reject",
        ["Reconciliation.MatchSelected"] = "Match selected",
        ["Reconciliation.SuggestMatches"] = "Suggest matches",
        ["Reconciliation.ExportCsv"] = "Export to CSV",
        ["Reconciliation.RecentMatches"] = "Recent matches",
        ["Reconciliation.MatchedVsUnmatched"] = "Matched / unmatched",
        ["Reconciliation.History"] = "Match history",
        ["Reconciliation.FilterByStatus"] = "Filter by status",
        ["Reconciliation.FilterAll"] = "All",
        ["Reconciliation.Column_Date"] = "Date",
        ["Reconciliation.Column_Amount"] = "Amount",
        ["Reconciliation.Column_Counterparty"] = "Counterparty",
        ["Reconciliation.Column_Description"] = "Description",
        ["Reconciliation.Column_Type"] = "Type",
        ["Reconciliation.Column_Status"] = "Status",
        ["Reconciliation.Column_Invoice"] = "Invoice",
        ["Reconciliation.Column_Transaction"] = "Transaction",
        ["Reconciliation.Status_Confirmed"] = "Confirmed",
        ["Reconciliation.Status_Pending"] = "Pending",
        ["Reconciliation.Status_Rejected"] = "Rejected",
        ["Reconciliation.Type_Automatic"] = "Automatic",
        ["Reconciliation.Type_Manual"] = "Manual",
        ["Reconciliation.Type_Suggested"] = "Suggested",
        ["Reconciliation.DateRange"] = "Date range",
        ["Reconciliation.DateFrom"] = "From",
        ["Reconciliation.DateTo"] = "To",
        ["Reconciliation.NoUnmatchedInvoices"] = "There are no unmatched invoices.",
        ["Reconciliation.NoUnmatchedTransactions"] = "There are no unmatched transactions.",
        ["Reconciliation.NoMatches"] = "No matches yet.",
        ["Reconciliation.SelectBothSides"] = "Select one invoice and one transaction.",
        ["Reconciliation.RunCompleted"] = "Completed: {0} automatic, {1} suggested.",
        ["Reconciliation.MatchConfirmed"] = "The match was confirmed.",
        ["Reconciliation.MatchRejected"] = "The match was rejected.",
        ["Reconciliation.MatchCreated"] = "The match was created.",
        ["Reconciliation.Error_AlreadyMatched"] = "This pair has already been matched.",
        ["Reconciliation.Error_SideAlreadyReconciled"] = "The invoice or the transaction is already matched to something else.",
        ["Reconciliation.Error_NotPending"] = "This match is no longer pending.",
        ["Reconciliation.Error_InvalidDateRange"] = "The “To” date must be after the “From” date.",
    });
}
