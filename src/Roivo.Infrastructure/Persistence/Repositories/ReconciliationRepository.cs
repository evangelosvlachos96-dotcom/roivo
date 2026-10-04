using Microsoft.EntityFrameworkCore;
using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Enums;

namespace Roivo.Infrastructure.Persistence.Repositories;

public sealed class ReconciliationRepository : IReconciliationRepository
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public ReconciliationRepository(IDbContextFactory<ApplicationDbContext> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    public async Task<IReadOnlyList<Invoice>> ListUnreconciledInvoicesAsync(
        Guid businessId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        return await UnreconciledInvoices(db, businessId)
            .Where(i => i.IssueDate >= from && i.IssueDate <= to)
            .OrderBy(i => i.IssueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BankTransaction>> ListUnreconciledTransactionsAsync(
        Guid businessId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        return await UnreconciledTransactions(db, businessId)
            .Where(t => t.BookingDate >= from && t.BookingDate <= to)
            .OrderBy(t => t.BookingDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<(Guid InvoiceId, Guid BankTransactionId)>> ListRejectedPairsAsync(
        Guid businessId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        var rows = await db.ReconciliationMatches
            .IgnoreQueryFilters()
            .Where(m => m.BusinessId == businessId && m.Status == ReconciliationMatchStatus.Rejected)
            .Select(m => new { m.InvoiceId, m.BankTransactionId })
            .ToListAsync(cancellationToken);

        return [.. rows.Select(r => (r.InvoiceId, r.BankTransactionId))];
    }

    public async Task<ReconciliationRule?> GetActiveRuleAsync(
        Guid businessId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        return await db.ReconciliationRules
            .IgnoreQueryFilters()
            .Where(r => r.BusinessId == businessId && r.IsActive)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ReconciliationMatch?> GetMatchByIdAsync(
        Guid matchId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.ReconciliationMatches.FirstOrDefaultAsync(m => m.Id == matchId, cancellationToken);
    }

    public async Task<int> AddMatchesAsync(
        IReadOnlyCollection<ReconciliationMatch> matches, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(matches);

        if (matches.Count == 0)
            return 0;

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        var invoiceIds = matches.Select(m => m.InvoiceId).Distinct().ToList();
        var transactionIds = matches.Select(m => m.BankTransactionId).Distinct().ToList();

        // One query for the batch: a nightly run over 90 days proposes hundreds
        // of pairs and per-row existence checks would dominate.
        var existing = await db.ReconciliationMatches
            .IgnoreQueryFilters()
            .Where(m => m.Status != ReconciliationMatchStatus.Rejected
                && (invoiceIds.Contains(m.InvoiceId) || transactionIds.Contains(m.BankTransactionId)))
            .Select(m => new { m.InvoiceId, m.BankTransactionId })
            .ToListAsync(cancellationToken);

        var claimedInvoices = existing.Select(e => e.InvoiceId).ToHashSet();
        var claimedTransactions = existing.Select(e => e.BankTransactionId).ToHashSet();

        var accepted = new List<ReconciliationMatch>(matches.Count);
        foreach (var match in matches)
        {
            // Either side already spoken for means a concurrent run or a manual
            // match got there first; skipping keeps the unique index from
            // turning a routine race into a failed job.
            if (!claimedInvoices.Add(match.InvoiceId))
                continue;

            if (!claimedTransactions.Add(match.BankTransactionId))
            {
                claimedInvoices.Remove(match.InvoiceId);
                continue;
            }

            accepted.Add(match);
        }

        if (accepted.Count == 0)
            return 0;

        db.ReconciliationMatches.AddRange(accepted);

        var confirmed = accepted.Where(m => m.IsEffective).ToList();
        if (confirmed.Count > 0)
            await ApplyReconciledFlagsAsync(db, confirmed, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        return accepted.Count;
    }

    public async Task UpdateMatchAsync(ReconciliationMatch match, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(match);

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        db.ReconciliationMatches.Update(match);

        if (match.IsEffective)
        {
            await ApplyReconciledFlagsAsync(db, [match], cancellationToken);
        }
        else
        {
            // A rejection releases both sides so they return to the queue and
            // can be matched against something else.
            await ClearReconciledFlagsAsync(db, match, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> PairIsMatchedAsync(
        Guid invoiceId, Guid bankTransactionId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        return await db.ReconciliationMatches
            .IgnoreQueryFilters()
            .AnyAsync(m => m.InvoiceId == invoiceId
                && m.BankTransactionId == bankTransactionId
                && m.Status != ReconciliationMatchStatus.Rejected, cancellationToken);
    }

    public async Task<bool> EitherSideIsReconciledAsync(
        Guid invoiceId, Guid bankTransactionId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        return await db.ReconciliationMatches
            .IgnoreQueryFilters()
            .AnyAsync(m => m.Status == ReconciliationMatchStatus.Confirmed
                && (m.InvoiceId == invoiceId || m.BankTransactionId == bankTransactionId), cancellationToken);
    }

    public async Task<IReadOnlyList<ReconciliationMatchDetail>> ListMatchDetailsAsync(
        Guid businessId,
        ReconciliationMatchStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        var query = db.ReconciliationMatches
            .IgnoreQueryFilters()
            .Where(m => m.BusinessId == businessId);

        if (status is { } s)
            query = query.Where(m => m.Status == s);

        return await query
            .OrderByDescending(m => m.MatchedAt)
            .Skip(skip)
            .Take(take)
            .Join(db.Invoices.IgnoreQueryFilters(), m => m.InvoiceId, i => i.Id, (m, i) => new { m, i })
            .Join(db.BankTransactions.IgnoreQueryFilters(), x => x.m.BankTransactionId, t => t.Id,
                (x, t) => new ReconciliationMatchDetail(
                    x.m.Id,
                    x.m.InvoiceId,
                    x.m.BankTransactionId,
                    x.m.MatchType,
                    x.m.Status,
                    x.m.MatchConfidence,
                    x.m.MatchedAt,
                    x.i.IssueDate,
                    x.i.GrossAmount,
                    x.i.CounterpartyName,
                    t.BookingDate,
                    t.Amount,
                    t.CounterpartyName))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountMatchesAsync(
        Guid businessId, ReconciliationMatchStatus? status, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        var query = db.ReconciliationMatches.IgnoreQueryFilters().Where(m => m.BusinessId == businessId);
        if (status is { } s)
            query = query.Where(m => m.Status == s);

        return await query.CountAsync(cancellationToken);
    }

    public async Task<ReconciliationCounts> GetCountsAsync(
        Guid businessId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        var totalInvoices = await db.Invoices.IgnoreQueryFilters()
            .CountAsync(i => i.BusinessId == businessId && i.IssueDate >= from && i.IssueDate <= to, cancellationToken);

        var accountIds = await db.BankAccounts.IgnoreQueryFilters()
            .Where(a => a.BusinessId == businessId)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        var totalTransactions = await db.BankTransactions.IgnoreQueryFilters()
            .CountAsync(t => accountIds.Contains(t.BankAccountId)
                && t.BookingDate >= from && t.BookingDate <= to, cancellationToken);

        // Scoped to matches whose invoice falls in the window, because
        // TotalInvoices above is. Counting matches over all time against a
        // windowed denominator lets the rate exceed 100% — glaringly so on the
        // accountant report, whose window is a single month. The join mirrors
        // ReconciledAmount below, which already scopes this way.
        var confirmed = await db.ReconciliationMatches.IgnoreQueryFilters()
            .Where(m => m.BusinessId == businessId && m.Status == ReconciliationMatchStatus.Confirmed)
            .Join(db.Invoices.IgnoreQueryFilters(), m => m.InvoiceId, i => i.Id, (_, i) => i)
            .CountAsync(i => i.IssueDate >= from && i.IssueDate <= to, cancellationToken);

        var pending = await db.ReconciliationMatches.IgnoreQueryFilters()
            .Where(m => m.BusinessId == businessId && m.Status == ReconciliationMatchStatus.Pending)
            .Join(db.Invoices.IgnoreQueryFilters(), m => m.InvoiceId, i => i.Id, (_, i) => i)
            .CountAsync(i => i.IssueDate >= from && i.IssueDate <= to, cancellationToken);

        var unreconciledInvoices = await UnreconciledInvoices(db, businessId)
            .CountAsync(i => i.IssueDate >= from && i.IssueDate <= to, cancellationToken);

        var unreconciledTransactions = await UnreconciledTransactions(db, businessId)
            .CountAsync(t => t.BookingDate >= from && t.BookingDate <= to, cancellationToken);

        var reconciledAmount = await db.ReconciliationMatches.IgnoreQueryFilters()
            .Where(m => m.BusinessId == businessId && m.Status == ReconciliationMatchStatus.Confirmed)
            .Join(db.Invoices.IgnoreQueryFilters(), m => m.InvoiceId, i => i.Id, (_, i) => i)
            .Where(i => i.IssueDate >= from && i.IssueDate <= to)
            .SumAsync(i => (decimal?)i.GrossAmount, cancellationToken) ?? 0m;

        return new ReconciliationCounts(
            totalInvoices, totalTransactions, confirmed, pending,
            unreconciledInvoices, unreconciledTransactions, reconciledAmount);
    }

    public async Task<IReadOnlyList<Invoice>> ListUnreconciledInvoicesPagedAsync(
        Guid businessId, int skip, int take, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        return await UnreconciledInvoices(db, businessId)
            .OrderByDescending(i => i.IssueDate)
            .Skip(skip).Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BankTransaction>> ListUnreconciledTransactionsPagedAsync(
        Guid businessId, int skip, int take, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        return await UnreconciledTransactions(db, businessId)
            .OrderByDescending(t => t.BookingDate)
            .Skip(skip).Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<(Invoice? Invoice, BankTransaction? Transaction)> GetPairForBusinessAsync(
        Guid businessId, Guid invoiceId, Guid bankTransactionId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        var invoice = await db.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.BusinessId == businessId, cancellationToken);

        // Joined through BankAccount because BankTransaction has no BusinessId
        // of its own; the account is what ties it to a business.
        var transaction = await db.BankTransactions
            .Join(db.BankAccounts, t => t.BankAccountId, a => a.Id, (t, a) => new { t, a.BusinessId })
            .Where(x => x.t.Id == bankTransactionId && x.BusinessId == businessId)
            .Select(x => x.t)
            .FirstOrDefaultAsync(cancellationToken);

        return (invoice, transaction);
    }

    /// <summary>
    /// Cancelled invoices are excluded: AADE voided them, so there is no money
    /// to find and leaving them in would permanently depress the match rate.
    /// </summary>
    private static IQueryable<Invoice> UnreconciledInvoices(ApplicationDbContext db, Guid businessId)
        => db.Invoices
            .IgnoreQueryFilters()
            .Where(i => i.BusinessId == businessId && !i.IsReconciled && i.CancelledByMark == null);

    private static IQueryable<BankTransaction> UnreconciledTransactions(ApplicationDbContext db, Guid businessId)
        => db.BankTransactions
            .IgnoreQueryFilters()
            .Join(db.BankAccounts.IgnoreQueryFilters(), t => t.BankAccountId, a => a.Id,
                (t, a) => new { Transaction = t, a.BusinessId })
            .Where(x => x.BusinessId == businessId && !x.Transaction.IsReconciled)
            .Select(x => x.Transaction);

    private static async Task ApplyReconciledFlagsAsync(
        ApplicationDbContext db,
        IReadOnlyCollection<ReconciliationMatch> matches,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var invoiceIds = matches.Select(m => m.InvoiceId).ToList();
        var transactionIds = matches.Select(m => m.BankTransactionId).ToList();

        var invoices = await db.Invoices.IgnoreQueryFilters()
            .Where(i => invoiceIds.Contains(i.Id))
            .ToListAsync(cancellationToken);

        foreach (var invoice in invoices)
            invoice.MarkReconciled(now);

        var transactions = await db.BankTransactions.IgnoreQueryFilters()
            .Where(t => transactionIds.Contains(t.Id))
            .ToListAsync(cancellationToken);

        var invoiceByTransaction = matches.ToDictionary(m => m.BankTransactionId, m => m.InvoiceId);

        foreach (var transaction in transactions)
        {
            transaction.IsReconciled = true;
            transaction.ReconciledAt ??= now;
            transaction.MatchedInvoiceId = invoiceByTransaction[transaction.Id];
        }
    }

    private static async Task ClearReconciledFlagsAsync(
        ApplicationDbContext db, ReconciliationMatch match, CancellationToken cancellationToken)
    {
        var invoice = await db.Invoices.IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == match.InvoiceId, cancellationToken);
        invoice?.ClearReconciliation();

        var transaction = await db.BankTransactions.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == match.BankTransactionId, cancellationToken);

        if (transaction is not null)
        {
            transaction.IsReconciled = false;
            transaction.ReconciledAt = null;
            transaction.MatchedInvoiceId = null;
        }
    }
}
