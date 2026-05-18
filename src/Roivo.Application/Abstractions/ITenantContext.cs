using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Abstractions;

/// <summary>
/// Resolves the current request's tenant identity. Implementations source the
/// values from the request principal's <c>tenant_id</c> and <c>tenant_type</c>
/// claims (or equivalents).
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// The current tenant identifier. Returns <see cref="Guid.Empty"/> when no
    /// tenant is in scope (e.g., anonymous request). Global query filters use
    /// equality against this value, so the empty sentinel safely returns no
    /// data for non-tenant-scoped callers.
    /// </summary>
    Guid CurrentTenantId { get; }

    /// <summary>
    /// The current tenant's type. Implementations default to
    /// <see cref="TenantType.Business"/> when the claim is missing or unparseable —
    /// the more restrictive option (see <c>BusinessPermissions</c>).
    /// </summary>
    TenantType CurrentTenantType { get; }
}
