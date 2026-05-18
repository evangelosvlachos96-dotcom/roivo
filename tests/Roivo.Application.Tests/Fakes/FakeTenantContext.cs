using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Tests.Fakes;

public sealed class FakeTenantContext : ITenantContext
{
    public Guid CurrentTenantId { get; set; } = Guid.NewGuid();

    // Default to Accountant so existing tests (which don't care about
    // permissions) keep passing. Permission-focused tests override.
    public TenantType CurrentTenantType { get; set; } = TenantType.Accountant;
}
