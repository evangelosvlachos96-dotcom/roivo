using Roivo.Application.Abstractions;
using Roivo.Application.Features.Reconciliation.Queries.GetReconciliationDashboard;

namespace Roivo.Application.Features.Reconciliation.Queries.ListReconciliationMatches;

public sealed class ListReconciliationMatchesHandler
{
    private const int MaxPageSize = 200;

    private readonly IReconciliationRepository _repository;

    public ListReconciliationMatchesHandler(IReconciliationRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public async Task<ReconciliationMatchesPage> Handle(
        ListReconciliationMatchesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var skip = (page - 1) * pageSize;

        var details = await _repository.ListMatchDetailsAsync(
            query.BusinessId, query.Status, skip, pageSize, cancellationToken);

        var total = await _repository.CountMatchesAsync(
            query.BusinessId, query.Status, cancellationToken);

        var items = details
            .Select(d => new ReconciliationMatchListItem(
                d.MatchId,
                d.InvoiceId,
                d.BankTransactionId,
                d.MatchType.ToString(),
                d.Status.ToString(),
                d.Confidence,
                d.MatchedAt,
                d.InvoiceIssueDate,
                d.InvoiceGrossAmount,
                d.InvoiceCounterpartyName,
                d.TransactionBookingDate,
                d.TransactionAmount,
                d.TransactionCounterpartyName))
            .ToList();

        return new ReconciliationMatchesPage(items, page, pageSize, total);
    }
}
