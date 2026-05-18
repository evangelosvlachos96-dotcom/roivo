using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Abstractions;

/// <summary>
/// Persistence boundary for <see cref="Business"/>. All methods are stateless
/// — each call opens, uses, and disposes its own DbContext. Write methods
/// (<see cref="AddAsync"/>, <see cref="UpdateAsync"/>) save atomically and
/// return the persisted entity. Read methods return detached entities.
/// </summary>
public interface IBusinessRepository
{
    Task<Business?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Business?> GetByIdActiveOnlyAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Business?> FindByAfmAsync(string afm, bool activeOnly, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Business>> ListActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns IDs of every active business across all tenants that has AADE
    /// credentials connected. Bypasses tenant filters — intended for the
    /// nightly sync cron, which runs system-wide.
    /// </summary>
    Task<IReadOnlyList<Guid>> ListIdsWithAadeCredentialsAcrossAllTenantsAsync(CancellationToken cancellationToken = default);

    Task<bool> AfmExistsAsync(string afm, bool activeOnly, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<Business> AddAsync(Business business, CancellationToken cancellationToken = default);
    Task<Business> UpdateAsync(Business business, CancellationToken cancellationToken = default);
}
