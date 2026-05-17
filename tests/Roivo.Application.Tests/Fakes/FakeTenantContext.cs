using Roivo.Application.Abstractions;

namespace Roivo.Application.Tests.Fakes;

public sealed class FakeTenantContext : ITenantContext
{
    public Guid? CurrentTenantId { get; set; } = Guid.NewGuid();
}
