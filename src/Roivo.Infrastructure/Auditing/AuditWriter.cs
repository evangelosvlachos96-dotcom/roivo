using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;
using Roivo.Infrastructure.Persistence;

namespace Roivo.Infrastructure.Auditing;

public sealed class AuditWriter : IAuditWriter
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditWriter> _logger;

    public AuditWriter(
        IDbContextFactory<ApplicationDbContext> factory,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditWriter> logger)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        ArgumentNullException.ThrowIfNull(logger);

        _factory = factory;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task WriteAsync(
        string action,
        Guid? userId = null,
        Guid? tenantId = null,
        string? entityType = null,
        string? entityId = null,
        object? details = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        try
        {
            // Serialize once at the boundary so callers pass anonymous objects
            // instead of pre-serialized JSON strings.
            var detailsJson = details switch
            {
                null => null,
                string s => s,
                _ => JsonSerializer.Serialize(details)
            };

            var entry = new AuditLog
            {
                Action = action,
                EntityType = entityType ?? string.Empty,
                EntityId = Guid.TryParse(entityId, out var parsed) ? parsed : Guid.Empty,
                UserId = userId,
                TenantId = tenantId,
                Details = detailsJson,
                IpAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
                Timestamp = DateTime.UtcNow,
            };

            await using var db = await _factory.CreateDbContextAsync(cancellationToken);
            db.AuditLogs.Add(entry);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Audit writes must never crash the originating request.
            _logger.LogError(ex, "Failed to write audit log entry for action {Action}", action);
        }
    }
}
