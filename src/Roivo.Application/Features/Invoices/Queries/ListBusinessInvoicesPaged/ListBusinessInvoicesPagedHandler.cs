using Roivo.Application.Abstractions;

namespace Roivo.Application.Features.Invoices.Queries.ListBusinessInvoicesPaged;

public sealed class ListBusinessInvoicesPagedHandler
{
    private const int MaxPageSize = 100;

    private readonly IBusinessRepository _businesses;
    private readonly IInvoiceQueryRepository _invoiceQueries;

    public ListBusinessInvoicesPagedHandler(
        IBusinessRepository businesses,
        IInvoiceQueryRepository invoiceQueries)
    {
        ArgumentNullException.ThrowIfNull(businesses);
        ArgumentNullException.ThrowIfNull(invoiceQueries);

        _businesses = businesses;
        _invoiceQueries = invoiceQueries;
    }

    public async Task<ListBusinessInvoicesPagedResult> Handle(ListBusinessInvoicesPagedQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var business = await _businesses.GetByIdActiveOnlyAsync(query.BusinessId, cancellationToken).ConfigureAwait(false);
        if (business is null)
            return new ListBusinessInvoicesPagedResult.BusinessNotFound();

        // Clamp paging into safe bounds rather than rejecting — the UI sends
        // whatever the table component reports, and out-of-range values are a
        // presentation glitch, not a caller error.
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var normalized = query with { Page = page, PageSize = pageSize };

        var (rows, totalCount) = await _invoiceQueries.ListPagedAsync(normalized, cancellationToken).ConfigureAwait(false);

        return new ListBusinessInvoicesPagedResult.Success(rows, totalCount);
    }
}
