using Microsoft.EntityFrameworkCore;
using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;

namespace Roivo.Infrastructure.Persistence.Repositories;

public sealed class BankTransactionRepository : IBankTransactionRepository
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public BankTransactionRepository(IDbContextFactory<ApplicationDbContext> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    public async Task<BankTransactionUpsertCounts> UpsertRangeAsync(
        IReadOnlyCollection<BankTransaction> transactions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transactions);

        if (transactions.Count == 0)
            return new BankTransactionUpsertCounts(0, 0);

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        // One query for the whole batch rather than one per transaction: a 90-day
        // backfill is thousands of rows, and per-row round-trips dominate.
        var accountIds = transactions.Select(t => t.BankAccountId).Distinct().ToList();
        var externalIds = transactions.Select(t => t.ExternalId).Distinct().ToList();

        var existing = await db.BankTransactions
            .IgnoreQueryFilters()
            .Where(t => accountIds.Contains(t.BankAccountId) && externalIds.Contains(t.ExternalId))
            .ToListAsync(cancellationToken);

        var existingByKey = existing.ToDictionary(t => (t.BankAccountId, t.ExternalId));

        var inserted = 0;
        var updated = 0;

        foreach (var incoming in transactions)
        {
            if (!existingByKey.TryGetValue((incoming.BankAccountId, incoming.ExternalId), out var stored))
            {
                db.BankTransactions.Add(incoming);
                // Guards against a duplicate inside the same batch, which a bank
                // restating a pending row can produce.
                existingByKey[(incoming.BankAccountId, incoming.ExternalId)] = incoming;
                inserted++;
                continue;
            }

            // Banks revise a transaction's amount, dates and description between
            // pending and booked, so an already-stored row is refreshed rather
            // than skipped. MatchedInvoiceId is deliberately untouched: it is
            // Roivo's own reconciliation state, not the bank's.
            stored.BookingDate = incoming.BookingDate;
            stored.ValueDate = incoming.ValueDate;
            stored.Amount = incoming.Amount;
            stored.Currency = incoming.Currency;
            stored.CounterpartyName = incoming.CounterpartyName;
            stored.CounterpartyIban = incoming.CounterpartyIban;
            stored.Reference = incoming.Reference;
            stored.RawPayload = incoming.RawPayload;
            updated++;
        }

        await db.SaveChangesAsync(cancellationToken);
        return new BankTransactionUpsertCounts(inserted, updated);
    }

    public async Task<int> CountByBusinessAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.BankTransactions
            .AsNoTracking()
            .CountAsync(t => t.BankAccount != null && t.BankAccount.BusinessId == businessId, cancellationToken);
    }
}
