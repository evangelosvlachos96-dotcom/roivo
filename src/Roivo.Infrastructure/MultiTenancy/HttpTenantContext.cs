using Microsoft.AspNetCore.Http;
using Roivo.Application.Abstractions;

namespace Roivo.Infrastructure.MultiTenancy;

public class HttpTenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    public Guid? CurrentTenantId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirst("tenant_id")?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }
}
