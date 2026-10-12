using Roivo.Application.Abstractions;

namespace Roivo.Application.Features.Reconciliation.Queries.GetUnreconciledItems;

public sealed class GetUnreconciledItemsHandler
{
    private const int MaxPageSize = 200;

    private readonly IReconciliationRepository _repository;

    public GetUnreconciledItemsHandler(IReconciliationRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public async Task<UnreconciledItemsPage> Handle(
        GetUnreconciledItemsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var skip = (page - 1) * pageSize;

        IReadOnlyList<UnreconciledItem> items;

        if (query.ItemType == UnreconciledItemType.Invoices)
        {
            var invoices = await _repository.ListUnreconciledInvoicesPagedAsync(
                query.BusinessId, skip, pageSize, cancellationToken);

            items = [.. invoices.Select(i => new UnreconciledItem(
                i.Id,
                i.IssueDate,
                i.GrossAmount,
                i.CounterpartyName ?? i.CounterpartyAfm,
                $"{i.InvoiceType} {i.Series}{i.Number}".Trim()))];
        }
        else
        {
            var transactions = await _repository.ListUnreconciledTransactionsPagedAsync(
                query.BusinessId, skip, pageSize, cancellationToken);

            items = [.. transactions.Select(t => new UnreconciledItem(
                t.Id,
                t.BookingDate,
                t.Amount,
                t.CounterpartyName,
                t.Reference))];
        }

        return new UnreconciledItemsPage(query.ItemType, items, page, pageSize);
    }
}
