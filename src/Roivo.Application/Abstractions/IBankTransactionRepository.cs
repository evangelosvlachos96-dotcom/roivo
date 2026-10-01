using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Abstractions;

/// <summary>
/// Persistence boundary for <see cref="BankTransaction"/>. Same stateless-factory
/// pattern as <see cref="IBusinessRepository"/>.
/// </summary>
public interface IBankTransactionRepository
{
    /// <summary>
    /// Inserts transactions that are new and updates those already stored,
    /// matching on (<see cref="BankTransaction.BankAccountId"/>,
    /// <see cref="BankTransaction.ExternalId"/>). Re-running a sync over the same
    /// window is therefore a no-op rather than a duplicate — banks restate
    /// pending rows, so overlapping windows are normal, not exceptional.
    /// </summary>
    /// <returns>How many rows were inserted and how many were updated.</returns>
    Task<BankTransactionUpsertCounts> UpsertRangeAsync(
        IReadOnlyCollection<BankTransaction> transactions,
        CancellationToken cancellationToken = default);

    Task<int> CountByBusinessAsync(Guid businessId, CancellationToken cancellationToken = default);
}

/// <summary>Result of a bulk transaction upsert.</summary>
public sealed record BankTransactionUpsertCounts(int Inserted, int Updated);
