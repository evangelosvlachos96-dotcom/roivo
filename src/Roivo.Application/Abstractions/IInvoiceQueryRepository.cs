using Roivo.Application.Features.Invoices.Queries.GetBusinessInvoiceSummary;
using Roivo.Application.Features.Invoices.Queries.ListBusinessInvoicesPaged;

namespace Roivo.Application.Abstractions;

/// <summary>
/// Read-side queries over synced AADE data (Invoices + IncomeBookEntries).
/// Aggregations run server-side in SQL; the detailed listing unions both tables
/// and paginates in the database. Same stateless-factory pattern as the other
/// repositories.
/// </summary>
public interface IInvoiceQueryRepository
{
    Task<InvoiceSummaryData> GetSummaryAsync(Guid businessId, CancellationToken ct = default);

    Task<(IReadOnlyList<InvoiceRow> Rows, int TotalCount)> ListPagedAsync(
        ListBusinessInvoicesPagedQuery query, CancellationToken ct = default);
}

/// <summary>
/// Repository-level summary projection. Excludes <c>LastSyncedAt</c> — that lives
/// on the <c>Business</c> entity and is composed into the handler result.
/// </summary>
public sealed record InvoiceSummaryData(
    decimal IncomingGrossTotal,
    int IncomingCount,
    decimal OutgoingGrossTotal,
    int OutgoingAggregateCount,
    int OutgoingInvoiceCount,
    IReadOnlyList<RecentInvoiceRow> Recent);
