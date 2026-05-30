using Roivo.Application.Abstractions;
using Roivo.Application.Features.Invoices.Queries.GetBusinessInvoiceSummary;
using Roivo.Application.Features.Invoices.Queries.ListBusinessInvoicesPaged;

namespace Roivo.Application.Tests.Fakes;

/// <summary>
/// In-memory <see cref="IInvoiceQueryRepository"/>. Holds a flat list of
/// <see cref="InvoiceRow"/> and applies the same filter/sort/paginate logic the
/// real EF repository runs in SQL, so handler tests exercise realistic behavior
/// without a database.
/// </summary>
public sealed class FakeInvoiceQueryRepository : IInvoiceQueryRepository
{
    public List<InvoiceRow> Rows { get; } = new();

    public Task<InvoiceSummaryData> GetSummaryAsync(Guid businessId, CancellationToken ct = default)
    {
        var incoming = Rows.Where(r => r.Direction == "Incoming").ToList();
        var outgoing = Rows.Where(r => r.Direction == "Outgoing").ToList();

        var recent = Rows
            .OrderByDescending(r => r.IssueDate)
            .Take(10)
            .Select(r => new RecentInvoiceRow(
                r.IssueDate,
                r.Direction,
                r.CounterpartyName ?? r.CounterpartyAfm,
                r.DocumentTypeCode,
                r.GrossAmount,
                r.IsCancelled,
                r.AggregateCount))
            .ToList();

        var data = new InvoiceSummaryData(
            IncomingGrossTotal: incoming.Sum(r => r.GrossAmount),
            IncomingCount: incoming.Count,
            OutgoingGrossTotal: outgoing.Sum(r => r.GrossAmount),
            OutgoingAggregateCount: outgoing.Count,
            OutgoingInvoiceCount: outgoing.Sum(r => r.AggregateCount ?? 0),
            Recent: recent);

        return Task.FromResult(data);
    }

    public Task<(IReadOnlyList<InvoiceRow> Rows, int TotalCount)> ListPagedAsync(
        ListBusinessInvoicesPagedQuery query, CancellationToken ct = default)
    {
        IEnumerable<InvoiceRow> q = Rows;

        q = query.Direction switch
        {
            InvoiceDirectionFilter.Incoming => q.Where(r => r.Direction == "Incoming"),
            InvoiceDirectionFilter.Outgoing => q.Where(r => r.Direction == "Outgoing"),
            _ => q,
        };

        if (query.FromDate is { } from)
            q = q.Where(r => r.IssueDate >= from.Date);

        if (query.ToDate is { } to)
            q = q.Where(r => r.IssueDate <= to.Date);

        if (!string.IsNullOrWhiteSpace(query.CounterpartyAfmSearch))
            q = q.Where(r => r.CounterpartyAfm.Contains(query.CounterpartyAfmSearch.Trim(), StringComparison.OrdinalIgnoreCase));

        q = query.CancelledStatus switch
        {
            CancelledFilter.HideCancelled => q.Where(r => !r.IsCancelled),
            CancelledFilter.OnlyCancelled => q.Where(r => r.IsCancelled),
            _ => q,
        };

        q = (query.SortColumn, query.SortDescending) switch
        {
            ("GrossAmount", true) => q.OrderByDescending(r => r.GrossAmount),
            ("GrossAmount", false) => q.OrderBy(r => r.GrossAmount),
            ("NetAmount", true) => q.OrderByDescending(r => r.NetAmount),
            ("NetAmount", false) => q.OrderBy(r => r.NetAmount),
            ("CounterpartyAfm", true) => q.OrderByDescending(r => r.CounterpartyAfm),
            ("CounterpartyAfm", false) => q.OrderBy(r => r.CounterpartyAfm),
            (_, false) => q.OrderBy(r => r.IssueDate),
            _ => q.OrderByDescending(r => r.IssueDate),
        };

        var materialized = q.ToList();
        var totalCount = materialized.Count;

        IReadOnlyList<InvoiceRow> page = materialized
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return Task.FromResult((page, totalCount));
    }
}
