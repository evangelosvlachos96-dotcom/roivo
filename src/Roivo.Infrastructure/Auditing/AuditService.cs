using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Roivo.Core.Domain.Entities;
using Roivo.Infrastructure.Persistence;

namespace Roivo.Infrastructure.Auditing;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        ApplicationDbContext db,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditService> logger)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task LogAsync(
        string action,
        Guid? userId = null,
        Guid? tenantId = null,
        string? entityType = null,
        string? entityId = null,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entry = new AuditLog
            {
                Action = action,
                EntityType = entityType ?? string.Empty,
                EntityId = Guid.TryParse(entityId, out var parsed) ? parsed : Guid.Empty,
                UserId = userId,
                TenantId = tenantId,
                Details = details,
                IpAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
                Timestamp = DateTime.UtcNow,
            };

            _db.AuditLogs.Add(entry);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log entry for action {Action}", action);
        }
    }
}
