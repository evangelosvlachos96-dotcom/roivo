using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Tests.Fakes;

/// <summary>
/// In-memory stand-in for <see cref="IReconciliationRepository"/>. Mirrors the
/// real repository where behaviour matters to a test: the reconciled flags move
/// with a match, and already-claimed pairs are skipped on insert.
/// </summary>
public sealed class FakeReconciliationRepository : IReconciliationRepository
{
    public List<Invoice> Invoices { get; } = [];
    public List<BankTransaction> Transactions { get; } = [];
    public List<ReconciliationMatch> Matches { get; } = [];
    public ReconciliationRule? ActiveRule { get; set; }
    public List<(Guid InvoiceId, Guid BankTransactionId)> RejectedPairs { get; } = [];

    /// <summary>Maps a transaction id to the business it belongs to, standing in for the account join.</summary>
    public Dictionary<Guid, Guid> TransactionBusiness { get; } = [];

    public Task<IReadOnlyList<Invoice>> ListUnreconciledInvoicesAsync(
        Guid businessId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Invoice>>(
            [.. Invoices.Where(i => i.BusinessId == businessId
                && !i.IsReconciled
                && i.CancelledByMark is null
                && i.IssueDate >= from && i.IssueDate <= to)]);

    public Task<IReadOnlyList<BankTransaction>> ListUnreconciledTransactionsAsync(
        Guid businessId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<BankTransaction>>(
            [.. Transactions.Where(t => BusinessOf(t) == businessId
                && !t.IsReconciled
                && t.BookingDate >= from && t.BookingDate <= to)]);

    public Task<IReadOnlyList<(Guid InvoiceId, Guid BankTransactionId)>> ListRejectedPairsAsync(
        Guid businessId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<(Guid, Guid)>>([.. RejectedPairs]);

    public Task<ReconciliationRule?> GetActiveRuleAsync(
        Guid businessId, CancellationToken cancellationToken = default)
        => Task.FromResult(ActiveRule);

    public Task<ReconciliationMatch?> GetMatchByIdAsync(Guid matchId, CancellationToken cancellationToken = default)
        => Task.FromResult(Matches.FirstOrDefault(m => m.Id == matchId));

    public Task<int> AddMatchesAsync(
        IReadOnlyCollection<ReconciliationMatch> matches, CancellationToken cancellationToken = default)
    {
        var inserted = 0;
        foreach (var match in matches)
        {
            var taken = Matches.Any(m => m.Status != ReconciliationMatchStatus.Rejected
                && (m.InvoiceId == match.InvoiceId || m.BankTransactionId == match.BankTransactionId));

            if (taken)
                continue;

            Matches.Add(match);
            inserted++;

            if (match.IsEffective)
                ApplyFlags(match);
        }

        return Task.FromResult(inserted);
    }

    public Task UpdateMatchAsync(ReconciliationMatch match, CancellationToken cancellationToken = default)
    {
        if (!Matches.Contains(match))
            Matches.Add(match);

        if (match.IsEffective)
            ApplyFlags(match);
        else
            ClearFlags(match);

        return Task.CompletedTask;
    }

    public Task<bool> PairIsMatchedAsync(
        Guid invoiceId, Guid bankTransactionId, CancellationToken cancellationToken = default)
        => Task.FromResult(Matches.Any(m => m.InvoiceId == invoiceId
            && m.BankTransactionId == bankTransactionId
            && m.Status != ReconciliationMatchStatus.Rejected));

    public Task<bool> EitherSideIsReconciledAsync(
        Guid invoiceId, Guid bankTransactionId, CancellationToken cancellationToken = default)
        => Task.FromResult(Matches.Any(m => m.Status == ReconciliationMatchStatus.Confirmed
            && (m.InvoiceId == invoiceId || m.BankTransactionId == bankTransactionId)));

    public Task<IReadOnlyList<ReconciliationMatchDetail>> ListMatchDetailsAsync(
        Guid businessId, ReconciliationMatchStatus? status, int skip, int take,
        CancellationToken cancellationToken = default)
    {
        var query = Matches.Where(m => m.BusinessId == businessId);
        if (status is { } s)
            query = query.Where(m => m.Status == s);

        var details = query
            .OrderByDescending(m => m.MatchedAt)
            .Skip(skip).Take(take)
            .Select(m =>
            {
                var invoice = Invoices.First(i => i.Id == m.InvoiceId);
                var transaction = Transactions.First(t => t.Id == m.BankTransactionId);
                return new ReconciliationMatchDetail(
                    m.Id, m.InvoiceId, m.BankTransactionId, m.MatchType, m.Status,
                    m.MatchConfidence, m.MatchedAt,
                    invoice.IssueDate, invoice.GrossAmount, invoice.CounterpartyName,
                    transaction.BookingDate, transaction.Amount, transaction.CounterpartyName);
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<ReconciliationMatchDetail>>(details);
    }

    public Task<int> CountMatchesAsync(
        Guid businessId, ReconciliationMatchStatus? status, CancellationToken cancellationToken = default)
        => Task.FromResult(Matches.Count(m => m.BusinessId == businessId
            && (status is null || m.Status == status)));

    public Task<ReconciliationCounts> GetCountsAsync(
        Guid businessId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var invoices = Invoices.Where(i => i.BusinessId == businessId
            && i.IssueDate >= from && i.IssueDate <= to).ToList();

        var transactions = Transactions.Where(t => BusinessOf(t) == businessId
            && t.BookingDate >= from && t.BookingDate <= to).ToList();

        var confirmed = Matches.Count(m => m.BusinessId == businessId
            && m.Status == ReconciliationMatchStatus.Confirmed);

        var pending = Matches.Count(m => m.BusinessId == businessId
            && m.Status == ReconciliationMatchStatus.Pending);

        var reconciledAmount = Matches
            .Where(m => m.BusinessId == businessId && m.Status == ReconciliationMatchStatus.Confirmed)
            .Join(invoices, m => m.InvoiceId, i => i.Id, (_, i) => i.GrossAmount)
            .Sum();

        return Task.FromResult(new ReconciliationCounts(
            invoices.Count,
            transactions.Count,
            confirmed,
            pending,
            invoices.Count(i => !i.IsReconciled && i.CancelledByMark is null),
            transactions.Count(t => !t.IsReconciled),
            reconciledAmount));
    }

    public Task<IReadOnlyList<Invoice>> ListUnreconciledInvoicesPagedAsync(
        Guid businessId, int skip, int take, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Invoice>>(
            [.. Invoices.Where(i => i.BusinessId == businessId && !i.IsReconciled)
                .OrderByDescending(i => i.IssueDate).Skip(skip).Take(take)]);

    public Task<IReadOnlyList<BankTransaction>> ListUnreconciledTransactionsPagedAsync(
        Guid businessId, int skip, int take, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<BankTransaction>>(
            [.. Transactions.Where(t => BusinessOf(t) == businessId && !t.IsReconciled)
                .OrderByDescending(t => t.BookingDate).Skip(skip).Take(take)]);

    public Task<(Invoice? Invoice, BankTransaction? Transaction)> GetPairForBusinessAsync(
        Guid businessId, Guid invoiceId, Guid bankTransactionId, CancellationToken cancellationToken = default)
    {
        var invoice = Invoices.FirstOrDefault(i => i.Id == invoiceId && i.BusinessId == businessId);
        var transaction = Transactions.FirstOrDefault(t => t.Id == bankTransactionId && BusinessOf(t) == businessId);
        return Task.FromResult((invoice, transaction));
    }

    private Guid BusinessOf(BankTransaction transaction)
        => TransactionBusiness.TryGetValue(transaction.Id, out var id) ? id : Guid.Empty;

    private void ApplyFlags(ReconciliationMatch match)
    {
        var now = DateTime.UtcNow;
        Invoices.FirstOrDefault(i => i.Id == match.InvoiceId)?.MarkReconciled(now);

        var transaction = Transactions.FirstOrDefault(t => t.Id == match.BankTransactionId);
        if (transaction is null)
            return;

        transaction.IsReconciled = true;
        transaction.ReconciledAt ??= now;
        transaction.MatchedInvoiceId = match.InvoiceId;
    }

    private void ClearFlags(ReconciliationMatch match)
    {
        Invoices.FirstOrDefault(i => i.Id == match.InvoiceId)?.ClearReconciliation();

        var transaction = Transactions.FirstOrDefault(t => t.Id == match.BankTransactionId);
        if (transaction is null)
            return;

        transaction.IsReconciled = false;
        transaction.ReconciledAt = null;
        transaction.MatchedInvoiceId = null;
    }
}
