using Roivo.Application.Abstractions;

namespace Roivo.Application.Features.Invoices.Queries.GetBusinessInvoiceSummary;

public sealed class GetBusinessInvoiceSummaryHandler
{
    private readonly IBusinessRepository _businesses;
    private readonly IInvoiceQueryRepository _invoiceQueries;

    public GetBusinessInvoiceSummaryHandler(
        IBusinessRepository businesses,
        IInvoiceQueryRepository invoiceQueries)
    {
        ArgumentNullException.ThrowIfNull(businesses);
        ArgumentNullException.ThrowIfNull(invoiceQueries);

        _businesses = businesses;
        _invoiceQueries = invoiceQueries;
    }

    public async Task<GetBusinessInvoiceSummaryResult> Handle(GetBusinessInvoiceSummaryQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var business = await _businesses.GetByIdActiveOnlyAsync(query.BusinessId, cancellationToken).ConfigureAwait(false);
        if (business is null)
            return new GetBusinessInvoiceSummaryResult.BusinessNotFound();

        var summary = await _invoiceQueries.GetSummaryAsync(business.Id, cancellationToken).ConfigureAwait(false);

        var data = new GetBusinessInvoiceSummaryData(
            IncomingGrossTotal: summary.IncomingGrossTotal,
            IncomingCount: summary.IncomingCount,
            OutgoingGrossTotal: summary.OutgoingGrossTotal,
            OutgoingAggregateCount: summary.OutgoingAggregateCount,
            OutgoingInvoiceCount: summary.OutgoingInvoiceCount,
            LastSyncedAt: business.LastAadeSyncAt,
            Recent: summary.Recent);

        return new GetBusinessInvoiceSummaryResult.Success(
            data,
            new BusinessHeaderInfo(
                business.Id,
                business.Name,
                business.Afm,
                business.HasAadeFailure,
                business.AadeLastFailureAt,
                business.AadeLastFailureReason));
    }
}
