using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Roivo.Core.Domain.Entities;
using Roivo.Infrastructure.Persistence;

namespace Roivo.Infrastructure.Identity;

// Claims are for authorization decisions; display data comes from the DB.
// tenant_id is included because ITenantContext / global query filters depend on it
// being available on every request without a DB round-trip. tenant_type is included
// so BusinessPermissions checks (used by handlers + UI) don't need a DB lookup either.
public class TenantClaimsPrincipalFactory
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole<Guid>>
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    public TenantClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IOptions<IdentityOptions> optionsAccessor,
        IDbContextFactory<ApplicationDbContext> dbContextFactory)
        : base(userManager, roleManager, optionsAccessor)
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        _dbContextFactory = dbContextFactory;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim("tenant_id", user.TenantId.ToString()));

        // Direct factory query to avoid sharing context with UserManager / UserStore.
        // The factory creates an isolated context that won't race with Identity's
        // internal queries during sign-in or security stamp validation.
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var tenantType = await db.Tenants
            .Where(t => t.Id == user.TenantId)
            .Select(t => (Roivo.Core.Domain.Enums.TenantType?)t.Type)
            .FirstOrDefaultAsync();

        if (tenantType.HasValue)
        {
            identity.AddClaim(new Claim("tenant_type", tenantType.Value.ToString()));
        }

        return identity;
    }
}
