using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Abstractions;

/// <summary>Persistence boundary for <see cref="Tenant"/> entities.</summary>
public interface ITenantRepository
{
    /// <summary>
    /// Loads a tenant by id, bypassing the global tenant query filter so a
    /// user can always read their own tenant row.
    /// </summary>
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts all tenants across the platform, bypassing tenant scope. Used
    /// for connectivity smoke checks; not for user-facing tenant data.
    /// </summary>
    Task<int> CountAllAsync(CancellationToken cancellationToken = default);
}
