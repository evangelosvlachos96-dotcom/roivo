namespace Roivo.Application.Abstractions;

/// <summary>
/// Writes audit-log entries. Implementations serialize the <c>details</c>
/// argument internally and must never throw — audit failures degrade silently
/// so the originating request always completes.
/// </summary>
public interface IAuditWriter
{
    Task WriteAsync(
        string action,
        Guid? userId = null,
        Guid? tenantId = null,
        string? entityType = null,
        string? entityId = null,
        object? details = null,
        CancellationToken cancellationToken = default);
}
