namespace Roivo.Infrastructure.MultiTenancy;

public interface ITenantContext
{
    Guid? CurrentTenantId { get; }
}
