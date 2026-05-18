using Roivo.Core.Domain.Enums;

namespace Roivo.Core.Domain.Businesses;

/// <summary>
/// Source of truth for who can do what with Business entities. Pure functions,
/// no I/O. Handlers consult these before mutating; UI consults these to decide
/// which controls to render.
/// </summary>
public static class BusinessPermissions
{
    /// <summary>
    /// True if a tenant of the given type is allowed to create new businesses.
    /// Accountants manage many client businesses. Business-owner tenants
    /// represent a single business that was created at registration — they
    /// cannot create additional ones.
    /// </summary>
    public static bool CanCreate(TenantType tenantType)
        => tenantType == TenantType.Accountant;

    /// <summary>
    /// True if a tenant of the given type is allowed to deactivate a business.
    /// Business-owner tenants cannot deactivate their primary business (would
    /// orphan their account).
    /// </summary>
    public static bool CanDeactivate(TenantType tenantType)
        => tenantType == TenantType.Accountant;

    /// <summary>
    /// True if a tenant of the given type is allowed to reactivate a
    /// deactivated business.
    /// </summary>
    public static bool CanReactivate(TenantType tenantType)
        => tenantType == TenantType.Accountant;

    /// <summary>
    /// True if a tenant of the given type is allowed to change a business's AFM.
    /// For business-owner tenants, the AFM is their tax identity and is immutable
    /// after registration. Accountants can change client AFMs (e.g., when a
    /// business legally restructures).
    /// </summary>
    public static bool CanEditAfm(TenantType tenantType)
        => tenantType == TenantType.Accountant;

    /// <summary>
    /// True if a tenant of the given type may manage AADE credentials for a
    /// business. Both tenant types qualify in M4; the UI further restricts
    /// business-tenants to their primary business.
    /// </summary>
    public static bool CanManageAadeFor(TenantType tenantType) => true;
}
