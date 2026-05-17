using Roivo.Application.Abstractions;

namespace Roivo.Application.Tests.Fakes;

public record AuditCall(string Action, Guid? TenantId, string? EntityType, string? EntityId, object? Details);

public sealed class FakeAuditWriter : IAuditWriter
{
    public List<AuditCall> Calls { get; } = new();

    public Task WriteAsync(
        string action,
        Guid? userId = null,
        Guid? tenantId = null,
        string? entityType = null,
        string? entityId = null,
        object? details = null,
        CancellationToken cancellationToken = default)
    {
        Calls.Add(new AuditCall(action, tenantId, entityType, entityId, details));
        return Task.CompletedTask;
    }
}
