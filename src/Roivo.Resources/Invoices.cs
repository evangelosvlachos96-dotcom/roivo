namespace Roivo.Resources;

/// <summary>
/// User-facing strings for the AADE invoice / cashflow views (summary + detailed
/// tabs on the per-business invoices page).
/// </summary>
public static class Invoices
{
    // Page sections
    public static string SummaryTab => Strings.Get("Invoices.SummaryTab", "Σύνοψη");
    public static string DetailedTab => Strings.Get("Invoices.DetailedTab", "Αναλυτικά");

    // Summary
    public static string NonReconciledDisclaimer => Strings.Get("Invoices.NonReconciledDisclaimer", "Στοιχεία από τιμολόγια AADE — μη συμφωνημένα με τραπεζικές κινήσεις. Συμφωνία θα γίνει αργότερα.");
    public static string IncomingTotalsHeading => Strings.Get("Invoices.IncomingTotalsHeading", "Εισερχόμενα");
    public static string OutgoingTotalsHeading => Strings.Get("Invoices.OutgoingTotalsHeading", "Εξερχόμενα");
    public static string NetFlowHeading => Strings.Get("Invoices.NetFlowHeading", "Καθαρή ροή");
    public static string NetFlowFormula => Strings.Get("Invoices.NetFlowFormula", "Εξερχόμενα - Εισερχόμενα");
    public static string RecentActivityHeading => Strings.Get("Invoices.RecentActivityHeading", "Τελευταία δραστηριότητα");
    public static string InvoiceCountLine => Strings.Get("Invoices.InvoiceCountLine", "{0} τιμολόγια");
    public static string AggregateCountLine => Strings.Get("Invoices.AggregateCountLine", "{0} εγγραφές, {1} τιμολόγια");
    public static string LastSyncedLabel => Strings.Get("Invoices.LastSyncedLabel", "Τελευταίος συγχρονισμός: {0:dd MMM yyyy HH:mm}");

    // Direction labels
    public static string DirectionIncoming => Strings.Get("Invoices.DirectionIncoming", "Εισερχόμενο");
    public static string DirectionOutgoing => Strings.Get("Invoices.DirectionOutgoing", "Εξερχόμενο");

    // Detailed table — filter labels
    public static string DirectionFilter => Strings.Get("Invoices.DirectionFilter", "Κατεύθυνση");
    public static string DirectionAll => Strings.Get("Invoices.DirectionAll", "Όλα");
    public static string FromDateLabel => Strings.Get("Invoices.FromDateLabel", "Από");
    public static string ToDateLabel => Strings.Get("Invoices.ToDateLabel", "Έως");
    public static string CounterpartySearchLabel => Strings.Get("Invoices.CounterpartySearchLabel", "Αναζήτηση ΑΦΜ");
    public static string CancelledFilterLabel => Strings.Get("Invoices.CancelledFilterLabel", "Ακυρωμένα");
    public static string CancelledAll => Strings.Get("Invoices.CancelledAll", "Όλα");
    public static string CancelledHide => Strings.Get("Invoices.CancelledHide", "Απόκρυψη ακυρωμένων");
    public static string CancelledOnly => Strings.Get("Invoices.CancelledOnly", "Μόνο ακυρωμένα");

    // Table columns
    public static string IssueDateColumn => Strings.Get("Invoices.IssueDateColumn", "Ημερομηνία");
    public static string DirectionColumn => Strings.Get("Invoices.DirectionColumn", "Κατεύθυνση");
    public static string CounterpartyColumn => Strings.Get("Invoices.CounterpartyColumn", "Αντισυμβαλλόμενος");
    public static string TypeColumn => Strings.Get("Invoices.TypeColumn", "Τύπος");
    public static string NetColumn => Strings.Get("Invoices.NetColumn", "Καθαρή αξία");
    public static string VatColumn => Strings.Get("Invoices.VatColumn", "ΦΠΑ");
    public static string GrossColumn => Strings.Get("Invoices.GrossColumn", "Συνολική αξία");
    public static string StatusColumn => Strings.Get("Invoices.StatusColumn", "Κατάσταση");

    // Status
    public static string StatusCancelled => Strings.Get("Invoices.StatusCancelled", "Ακυρωμένο");

    // Navigation
    public static string ViewInvoicesLink => Strings.Get("Invoices.ViewInvoicesLink", "Τιμολόγια");

    /// <summary>English overrides. Keys absent here fall back to Greek.</summary>
    static Invoices() => Strings.RegisterEnglish(new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Invoices.SummaryTab"] = "Summary",
        ["Invoices.DetailedTab"] = "Details",
        ["Invoices.NonReconciledDisclaimer"] = "Figures from AADE invoices — not yet reconciled with bank transactions. Reconciliation comes later.",
        ["Invoices.IncomingTotalsHeading"] = "Incoming",
        ["Invoices.OutgoingTotalsHeading"] = "Outgoing",
        ["Invoices.NetFlowHeading"] = "Net flow",
        ["Invoices.NetFlowFormula"] = "Outgoing - Incoming",
        ["Invoices.RecentActivityHeading"] = "Recent activity",
        ["Invoices.InvoiceCountLine"] = "{0} invoices",
        ["Invoices.AggregateCountLine"] = "{0} records, {1} invoices",
        ["Invoices.LastSyncedLabel"] = "Last sync: {0:dd MMM yyyy HH:mm}",
        ["Invoices.DirectionIncoming"] = "Incoming",
        ["Invoices.DirectionOutgoing"] = "Outgoing",
        ["Invoices.DirectionFilter"] = "Direction",
        ["Invoices.DirectionAll"] = "All",
        ["Invoices.FromDateLabel"] = "From",
        ["Invoices.ToDateLabel"] = "To",
        ["Invoices.CounterpartySearchLabel"] = "Search VAT number",
        ["Invoices.CancelledFilterLabel"] = "Cancelled",
        ["Invoices.CancelledAll"] = "All",
        ["Invoices.CancelledHide"] = "Hide cancelled",
        ["Invoices.CancelledOnly"] = "Cancelled only",
        ["Invoices.IssueDateColumn"] = "Date",
        ["Invoices.DirectionColumn"] = "Direction",
        ["Invoices.CounterpartyColumn"] = "Counterparty",
        ["Invoices.TypeColumn"] = "Type",
        ["Invoices.NetColumn"] = "Net",
        ["Invoices.VatColumn"] = "VAT",
        ["Invoices.GrossColumn"] = "Gross",
        ["Invoices.StatusColumn"] = "Status",
        ["Invoices.StatusCancelled"] = "Cancelled",
        ["Invoices.ViewInvoicesLink"] = "Invoices",
    });
}
