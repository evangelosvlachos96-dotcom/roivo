namespace Roivo.Application.Features.Invoices.Queries.ListBusinessInvoicesPaged;

public abstract record ListBusinessInvoicesPagedResult
{
    public sealed record Success(IReadOnlyList<InvoiceRow> Rows, int TotalCount) : ListBusinessInvoicesPagedResult;
    public sealed record BusinessNotFound : ListBusinessInvoicesPagedResult;
}

/// <summary>
/// A single row in the unified detailed view. Incoming rows come from
/// <c>Invoices</c> (per-document); outgoing rows come from <c>IncomeBookEntries</c>
/// (daily aggregates), where <see cref="AggregateCount"/> is the invoice count
/// behind the aggregate and <see cref="Mark"/> / <see cref="CounterpartyName"/>
/// are null (not available for aggregates).
/// </summary>
public sealed record InvoiceRow(
    DateTime IssueDate,
    string Direction,
    string? Mark,
    string CounterpartyAfm,
    string? CounterpartyName,
    string DocumentTypeCode,
    decimal NetAmount,
    decimal VatAmount,
    decimal GrossAmount,
    bool IsCancelled,
    int? AggregateCount);
