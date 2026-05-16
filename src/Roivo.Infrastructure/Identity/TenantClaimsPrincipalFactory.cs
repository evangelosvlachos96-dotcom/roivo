using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Roivo.Core.Domain.Entities;

namespace Roivo.Infrastructure.Identity;

// Claims are for authorization decisions; display data comes from the DB.
// tenant_id is included because ITenantContext / global query filters depend on it
// being available on every request without a DB round-trip. Anything that can change
// at runtime (full name, email, etc.) is fetched fresh from the DB to avoid stale-data
// bugs after profile updates.
public class TenantClaimsPrincipalFactory
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole<Guid>>
{
    public TenantClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, roleManager, optionsAccessor)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim("tenant_id", user.TenantId.ToString()));
        return identity;
    }
}
