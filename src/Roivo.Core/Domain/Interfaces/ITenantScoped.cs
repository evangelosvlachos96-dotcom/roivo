namespace Roivo.Core.Domain.Interfaces;

public interface ITenantScoped
{
    Guid TenantId { get; set; }
}
