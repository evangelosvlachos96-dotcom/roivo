namespace Roivo.Core.Domain;

public interface ITenantScoped
{
    Guid TenantId { get; set; }
}
