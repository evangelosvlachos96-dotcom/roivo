using Roivo.Core.Domain.Interfaces;

namespace Roivo.Core.Domain.Entities;

public class Business : ITenantScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public required string Name { get; set; }
    public required string Afm { get; set; }
    public string? Kad { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? AadeUserId { get; set; }
    public string? AadeSubscriptionKeyEncrypted { get; set; }
}
