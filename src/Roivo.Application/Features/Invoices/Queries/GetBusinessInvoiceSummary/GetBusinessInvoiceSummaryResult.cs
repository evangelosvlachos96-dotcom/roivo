namespace Roivo.Application.Features.Invoices.Queries.GetBusinessInvoiceSummary;

public abstract record GetBusinessInvoiceSummaryResult
{
    public sealed record Success(GetBusinessInvoiceSummaryData Data, BusinessHeaderInfo Business) : GetBusinessInvoiceSummaryResult;
    public sealed record BusinessNotFound : GetBusinessInvoiceSummaryResult;
}

/// <summary>Totals + recent activity for a business's synced AADE data.</summary>
public sealed record GetBusinessInvoiceSummaryData(
    decimal IncomingGrossTotal,
    int IncomingCount,
    decimal OutgoingGrossTotal,
    int OutgoingAggregateCount,
    int OutgoingInvoiceCount,
    DateTime? LastSyncedAt,
    IReadOnlyList<RecentInvoiceRow> Recent);

public sealed record RecentInvoiceRow(
    DateTime IssueDate,
    string Direction,       // "Incoming" or "Outgoing"
    string CounterpartyDisplay,
    string DocumentTypeCode,
    decimal GrossAmount,
    bool IsCancelled,
    int? AggregateCount);   // null for incoming; N for outgoing aggregate rows

/// <summary>
/// Minimal business identity carried to the UI for page headers. Avoids passing
/// the <c>Business</c> entity across the Application/UI boundary. Failure fields
/// let the cashflow page render the same connection-failure banner shown on the
/// AADE page without a second round-trip to load the entity.
/// </summary>
public sealed record BusinessHeaderInfo(
    Guid Id,
    string Name,
    string Afm,
    bool HasAadeFailure,
    DateTime? AadeLastFailureAt,
    string? AadeLastFailureReason);
