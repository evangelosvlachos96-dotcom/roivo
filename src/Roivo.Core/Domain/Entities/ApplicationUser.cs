using Microsoft.AspNetCore.Identity;

namespace Roivo.Core.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public required string FullName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}
