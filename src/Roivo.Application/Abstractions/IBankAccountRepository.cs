using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Abstractions;

/// <summary>
/// Persistence boundary for <see cref="BankAccount"/>. Same stateless-factory
/// pattern as <see cref="IBusinessRepository"/>.
/// </summary>
public interface IBankAccountRepository
{
    Task<IReadOnlyList<BankAccount>> ListByBusinessAsync(Guid businessId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists a business's accounts without applying the tenant filter. For the
    /// nightly cron, which runs system-wide and has no tenant scope.
    /// </summary>
    Task<IReadOnlyList<BankAccount>> ListByBusinessAcrossAllTenantsAsync(Guid businessId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts or updates the account. Match is by
    /// (<see cref="BankAccount.BusinessId"/>, <see cref="BankAccount.ExternalAccountUid"/>),
    /// so re-consenting to the same accounts updates rows instead of duplicating
    /// them — and keeps the transactions already attached to them.
    /// </summary>
    Task<BankAccount> UpsertAsync(BankAccount account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes every account of a business, and with them (by cascade) their
    /// transactions. Used when a user disconnects.
    /// </summary>
    Task DeleteByBusinessAsync(Guid businessId, CancellationToken cancellationToken = default);
}
