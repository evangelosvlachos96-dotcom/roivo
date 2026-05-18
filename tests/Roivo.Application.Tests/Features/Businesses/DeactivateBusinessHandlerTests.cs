using FluentAssertions;
using Roivo.Application.Features.Businesses.Commands.DeactivateBusiness;
using Roivo.Application.Tests.Fakes;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Tests.Features.Businesses;

public class DeactivateBusinessHandlerTests
{
    private const string ValidAfm = "094014201";

    private static (DeactivateBusinessHandler handler, FakeBusinessRepository repo, FakeAuditWriter audit) BuildSut(TenantType tenantType = TenantType.Accountant)
    {
        var repo = new FakeBusinessRepository();
        var audit = new FakeAuditWriter();
        var tenant = new FakeTenantContext { CurrentTenantType = tenantType };
        return (new DeactivateBusinessHandler(repo, audit, tenant), repo, audit);
    }

    [Fact]
    public async Task Happy_path_flips_IsActive_and_writes_audit()
    {
        var (handler, repo, audit) = BuildSut();
        var b = Business.Create("Acme", ValidAfm, null, null);
        repo.Store[b.Id] = b;

        var result = await handler.Handle(new DeactivateBusinessCommand(b.Id));

        result.Should().BeOfType<DeactivateBusinessResult.Success>();
        repo.Store[b.Id].IsActive.Should().BeFalse();
        audit.Calls.Should().ContainSingle().Which.Action.Should().Be("BusinessDeactivated");
    }

    [Fact]
    public async Task Not_found_returns_NotFound()
    {
        var (handler, _, audit) = BuildSut();

        var result = await handler.Handle(new DeactivateBusinessCommand(Guid.NewGuid()));

        result.Should().BeOfType<DeactivateBusinessResult.NotFound>();
        audit.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Already_inactive_returns_AlreadyInactive_without_audit()
    {
        var (handler, repo, audit) = BuildSut();
        var b = Business.Create("Acme", ValidAfm, null, null);
        b.Deactivate();
        repo.Store[b.Id] = b;

        var result = await handler.Handle(new DeactivateBusinessCommand(b.Id));

        result.Should().BeOfType<DeactivateBusinessResult.AlreadyInactive>();
        audit.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Forbidden_when_tenant_is_business_type()
    {
        var (handler, repo, audit) = BuildSut(TenantType.Business);
        var b = Business.Create("Acme", ValidAfm, null, null);
        repo.Store[b.Id] = b;

        var result = await handler.Handle(new DeactivateBusinessCommand(b.Id));

        result.Should().BeOfType<DeactivateBusinessResult.Forbidden>();
        repo.Store[b.Id].IsActive.Should().BeTrue();
        audit.Calls.Should().BeEmpty();
    }
}
