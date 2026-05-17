using Microsoft.AspNetCore.Http;
using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Enums;

namespace Roivo.Infrastructure.MultiTenancy;

public class HttpTenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpTenantContext(IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid CurrentTenantId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User.FindFirst("tenant_id")?.Value;
            return Guid.TryParse(value, out var id) ? id : Guid.Empty;
        }
    }

    public TenantType CurrentTenantType
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User.FindFirst("tenant_type")?.Value;
            // Default to Business — the more restrictive type — when the claim
            // is missing or unparseable, so a misconfigured token can't grant
            // accountant-only operations.
            return Enum.TryParse<TenantType>(value, out var type) ? type : TenantType.Business;
        }
    }
}
