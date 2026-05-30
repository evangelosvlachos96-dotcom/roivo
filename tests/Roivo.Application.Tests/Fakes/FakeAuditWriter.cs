using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Auditing;

namespace Roivo.Application.Tests.Fakes;

public record AuditCall(AuditAction Action, Guid? TenantId, string? EntityType, string? EntityId, object? Details);

public sealed class FakeAuditWriter : IAuditWriter
{
    public List<AuditCall> Calls { get; } = new();

    public Task WriteAsync(
        AuditAction action,
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
