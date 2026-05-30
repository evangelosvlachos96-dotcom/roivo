namespace Roivo.Resources;

/// <summary>
/// User-facing strings for the AADE invoice / cashflow views (summary + detailed
/// tabs on the per-business invoices page).
/// </summary>
public static class Invoices
{
    // Page sections
    public const string SummaryTab = "Σύνοψη";
    public const string DetailedTab = "Αναλυτικά";

    // Summary
    public const string NonReconciledDisclaimer = "Στοιχεία από τιμολόγια AADE — μη συμφωνημένα με τραπεζικές κινήσεις. Συμφωνία θα γίνει αργότερα.";
    public const string IncomingTotalsHeading = "Εισερχόμενα";
    public const string OutgoingTotalsHeading = "Εξερχόμενα";
    public const string NetFlowHeading = "Καθαρή ροή";
    public const string NetFlowFormula = "Εξερχόμενα - Εισερχόμενα";
    public const string RecentActivityHeading = "Τελευταία δραστηριότητα";
    public const string InvoiceCountLine = "{0} τιμολόγια";
    public const string AggregateCountLine = "{0} εγγραφές, {1} τιμολόγια";
    public const string LastSyncedLabel = "Τελευταίος συγχρονισμός: {0:dd MMM yyyy HH:mm}";

    // Direction labels
    public const string DirectionIncoming = "Εισερχόμενο";
    public const string DirectionOutgoing = "Εξερχόμενο";

    // Detailed table — filter labels
    public const string DirectionFilter = "Κατεύθυνση";
    public const string DirectionAll = "Όλα";
    public const string FromDateLabel = "Από";
    public const string ToDateLabel = "Έως";
    public const string CounterpartySearchLabel = "Αναζήτηση ΑΦΜ";
    public const string CancelledFilterLabel = "Ακυρωμένα";
    public const string CancelledAll = "Όλα";
    public const string CancelledHide = "Απόκρυψη ακυρωμένων";
    public const string CancelledOnly = "Μόνο ακυρωμένα";

    // Table columns
    public const string IssueDateColumn = "Ημερομηνία";
    public const string DirectionColumn = "Κατεύθυνση";
    public const string CounterpartyColumn = "Αντισυμβαλλόμενος";
    public const string TypeColumn = "Τύπος";
    public const string NetColumn = "Καθαρή αξία";
    public const string VatColumn = "ΦΠΑ";
    public const string GrossColumn = "Συνολική αξία";
    public const string StatusColumn = "Κατάσταση";

    // Status
    public const string StatusCancelled = "Ακυρωμένο";

    // Navigation
    public const string ViewInvoicesLink = "Τιμολόγια";
}
