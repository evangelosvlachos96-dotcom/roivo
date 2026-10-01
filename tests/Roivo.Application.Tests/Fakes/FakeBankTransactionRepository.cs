using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Tests.Fakes;

public sealed class FakeBankTransactionRepository : IBankTransactionRepository
{
    /// <summary>Stored rows keyed the same way the real index is, so the fake
    /// reproduces the production idempotency rule rather than an easier one.</summary>
    public Dictionary<(Guid BankAccountId, string ExternalId), BankTransaction> Store { get; } = new();

    public Task<BankTransactionUpsertCounts> UpsertRangeAsync(
        IReadOnlyCollection<BankTransaction> transactions,
        CancellationToken cancellationToken = default)
    {
        var inserted = 0;
        var updated = 0;

        foreach (var transaction in transactions)
        {
            var key = (transaction.BankAccountId, transaction.ExternalId);
            if (Store.ContainsKey(key)) updated++;
            else inserted++;

            Store[key] = transaction;
        }

        return Task.FromResult(new BankTransactionUpsertCounts(inserted, updated));
    }

    public Task<int> CountByBusinessAsync(Guid businessId, CancellationToken cancellationToken = default)
        => Task.FromResult(Store.Count);
}
