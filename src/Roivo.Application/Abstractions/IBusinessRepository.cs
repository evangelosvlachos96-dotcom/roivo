using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Abstractions;

/// <summary>
/// Persistence boundary for <see cref="Business"/> entities. Implementations
/// are responsible for tenant scoping; callers do not pass <c>TenantId</c>.
/// </summary>
public interface IBusinessRepository
{
    /// <summary>Loads a business by id without filtering on <c>IsActive</c>.</summary>
    Task<Business?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Loads an active business by id, returning null when missing or inactive.</summary>
    Task<Business?> GetByIdActiveOnlyAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a business by AFM. When <paramref name="activeOnly"/> is true,
    /// only active rows are considered.
    /// </summary>
    Task<Business?> FindByAfmAsync(string afm, bool activeOnly, CancellationToken cancellationToken = default);

    /// <summary>Returns active businesses for the current tenant, ordered by name.</summary>
    Task<IReadOnlyList<Business>> ListActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests whether a business with the given AFM exists. When
    /// <paramref name="excludeId"/> is non-null, that id is excluded from the
    /// check (used by update flows so a row doesn't conflict with itself).
    /// </summary>
    Task<bool> AfmExistsAsync(string afm, bool activeOnly, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>Stages a new business for insertion. Pair with <see cref="SaveChangesAsync"/>.</summary>
    Task AddAsync(Business business, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists field changes on <paramref name="business"/>. Self-contained:
    /// no preceding <see cref="SaveChangesAsync"/> call is required.
    /// </summary>
    Task UpdateAsync(Business business, CancellationToken cancellationToken = default);

    /// <summary>Persists pending adds staged via <see cref="AddAsync"/>.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
