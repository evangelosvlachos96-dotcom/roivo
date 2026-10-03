namespace Roivo.Resources;

/// <summary>User-facing strings for the reconciliation (M6) UI.</summary>
public static class Reconciliation
{
    // Page and navigation
    public const string Title = "Αντιστοίχιση";
    public const string Nav_Reconciliation = "Αντιστοίχιση";
    public const string Subtitle = "Σύνδεση τιμολογίων με τραπεζικές συναλλαγές.";

    // Match kinds
    public const string AutomaticMatching = "Αυτόματη αντιστοίχιση";
    public const string ManualMatching = "Χειροκίνητη αντιστοίχιση";
    public const string SuggestedMatches = "Προτεινόμενες αντιστοιχίσεις";

    // Unmatched
    public const string UnmatchedInvoices = "Μη αντιστοιχισμένα τιμολόγια";
    public const string UnmatchedTransactions = "Μη αντιστοιχισμένες συναλλαγές";

    // Metrics
    public const string MatchRate = "Ποσοστό αντιστοίχισης";
    public const string ReconciledAmount = "Αντιστοιχισμένο ποσό";
    public const string TotalInvoices = "Σύνολο τιμολογίων";
    public const string TotalTransactions = "Σύνολο συναλλαγών";
    public const string ConfirmedMatches = "Επιβεβαιωμένες αντιστοιχίσεις";
    public const string PendingMatches = "Εκκρεμείς αντιστοιχίσεις";
    public const string Confidence = "Βεβαιότητα";

    // Actions
    public const string RunReconciliation = "Εκτέλεση αντιστοίχισης";
    public const string Confirm = "Επιβεβαίωση";
    public const string Reject = "Απόρριψη";
    public const string MatchSelected = "Αντιστοίχιση επιλεγμένων";
    public const string SuggestMatches = "Πρόταση αντιστοιχίσεων";
    public const string ExportCsv = "Εξαγωγή σε CSV";

    // History
    public const string RecentMatches = "Πρόσφατες αντιστοιχίσεις";
    public const string MatchedVsUnmatched = "Αντιστοιχισμένα / μη αντιστοιχισμένα";
    public const string History = "Ιστορικό αντιστοιχίσεων";
    public const string FilterByStatus = "Φίλτρο κατάστασης";
    public const string FilterAll = "Όλες";

    // Table headers
    public const string Column_Date = "Ημερομηνία";
    public const string Column_Amount = "Ποσό";
    public const string Column_Counterparty = "Αντισυμβαλλόμενος";
    public const string Column_Description = "Περιγραφή";
    public const string Column_Type = "Τύπος";
    public const string Column_Status = "Κατάσταση";
    public const string Column_Invoice = "Τιμολόγιο";
    public const string Column_Transaction = "Συναλλαγή";

    // Statuses
    public const string Status_Confirmed = "Επιβεβαιωμένη";
    public const string Status_Pending = "Εκκρεμής";
    public const string Status_Rejected = "Απορριφθείσα";
    public const string Type_Automatic = "Αυτόματη";
    public const string Type_Manual = "Χειροκίνητη";
    public const string Type_Suggested = "Προτεινόμενη";

    // Date range
    public const string DateRange = "Χρονικό διάστημα";
    public const string DateFrom = "Από";
    public const string DateTo = "Έως";

    // Empty and result states
    public const string NoUnmatchedInvoices = "Δεν υπάρχουν μη αντιστοιχισμένα τιμολόγια.";
    public const string NoUnmatchedTransactions = "Δεν υπάρχουν μη αντιστοιχισμένες συναλλαγές.";
    public const string NoMatches = "Δεν υπάρχουν αντιστοιχίσεις ακόμη.";
    public const string SelectBothSides = "Επιλέξτε ένα τιμολόγιο και μία συναλλαγή.";

    /// <summary>Placeholders: automatic count, suggested count.</summary>
    public const string RunCompleted = "Ολοκληρώθηκε: {0} αυτόματες, {1} προτεινόμενες.";

    public const string MatchConfirmed = "Η αντιστοίχιση επιβεβαιώθηκε.";
    public const string MatchRejected = "Η αντιστοίχιση απορρίφθηκε.";
    public const string MatchCreated = "Η αντιστοίχιση δημιουργήθηκε.";

    // Errors
    public const string Error_AlreadyMatched = "Αυτό το ζεύγος έχει ήδη αντιστοιχιστεί.";
    public const string Error_SideAlreadyReconciled = "Το τιμολόγιο ή η συναλλαγή έχει ήδη αντιστοιχιστεί με κάτι άλλο.";
    public const string Error_NotPending = "Η αντιστοίχιση δεν εκκρεμεί πλέον.";
    public const string Error_InvalidDateRange = "Η ημερομηνία «Έως» πρέπει να είναι μετά την «Από».";
}
