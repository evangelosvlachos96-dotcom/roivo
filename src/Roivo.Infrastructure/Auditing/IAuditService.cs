namespace Roivo.Infrastructure.Auditing;

public interface IAuditService
{
    Task LogAsync(string action, Guid? userId = null, Guid? tenantId = null, string? entityType = null, string? entityId = null, string? details = null, CancellationToken cancellationToken = default);
}