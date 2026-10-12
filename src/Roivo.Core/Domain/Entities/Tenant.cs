using Roivo.Core.Domain.Enums;

namespace Roivo.Core.Domain.Entities;

public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Afm { get; set; }
    public TenantType Type { get; set; }

    /// <summary>
    /// What the tenant has paid for. Starter until something sets it —
    /// there is no billing yet, so nothing does.
    /// </summary>
    public PlanTier PlanTier { get; set; } = PlanTier.Starter;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
