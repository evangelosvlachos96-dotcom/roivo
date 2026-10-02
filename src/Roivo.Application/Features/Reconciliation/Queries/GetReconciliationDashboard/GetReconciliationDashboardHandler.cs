using Roivo.Application.Abstractions;

namespace Roivo.Application.Features.Reconciliation.Queries.GetReconciliationDashboard;

public sealed class GetReconciliationDashboardHandler
{
    /// <summary>Window the dashboard covers when the caller names no dates.</summary>
    public const int DefaultWindowDays = 90;

    private readonly IReconciliationRepository _repository;

    public GetReconciliationDashboardHandler(IReconciliationRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public async Task<ReconciliationDashboard> Handle(
        GetReconciliationDashboardQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var to = query.To ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var from = query.From ?? to.AddDays(-DefaultWindowDays);

        var counts = await _repository.GetCountsAsync(query.BusinessId, from, to, cancellationToken);

        var recent = await _repository.ListMatchDetailsAsync(
            query.BusinessId, status: null, skip: 0, take: query.RecentMatchCount, cancellationToken);

        // Rate is against invoices seen in the window, not against matches: an
        // accountant asks "how much of my paperwork is accounted for", and a
        // denominator of matches would always read 100%.
        var matchRate = counts.TotalInvoices == 0
            ? 0m
            : Math.Round((decimal)counts.ConfirmedMatches / counts.TotalInvoices, 4);

        return new ReconciliationDashboard(
            BusinessId: query.BusinessId,
            From: from,
            To: to,
            TotalInvoices: counts.TotalInvoices,
            TotalTransactions: counts.TotalTransactions,
            ConfirmedMatches: counts.ConfirmedMatches,
            PendingMatches: counts.PendingMatches,
            UnreconciledInvoices: counts.UnreconciledInvoices,
            UnreconciledTransactions: counts.UnreconciledTransactions,
            ReconciledAmount: counts.ReconciledAmount,
            MatchRate: matchRate,
            RecentMatches: [.. recent.Select(ToListItem)]);
    }

    private static ReconciliationMatchListItem ToListItem(ReconciliationMatchDetail d) => new(
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
        d.TransactionCounterpartyName);
}
