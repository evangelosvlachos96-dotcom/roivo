using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Features.Accountant;

/// <summary>
/// Who may reach the accountant-wide views. Pure functions, no I/O — the same
/// shape as <c>Roivo.Core.Domain.Businesses.BusinessPermissions</c>, which the
/// handlers consult before loading anything and the pages consult before
/// rendering.
/// </summary>
/// <remarks>
/// This lives in Application rather than beside <c>BusinessPermissions</c>
/// because the rule is about a cross-business workspace, not about a single
/// <c>Business</c> aggregate: there is no Core aggregate it belongs to.
/// </remarks>
public static class AccountantAccess
{
    /// <summary>
    /// True when a tenant of the given type may see the consolidated
    /// accountant dashboard, alerts and reports. Only accountant tenants
    /// manage more than one business, so for a business-owner tenant the whole
    /// surface is meaningless — and would leak the shape of a workspace they
    /// do not have.
    /// </summary>
    public static bool CanViewAccountantWorkspace(TenantType tenantType)
        => tenantType == TenantType.Accountant;
}
