using FluentAssertions;
using Roivo.Application.Features.Businesses.Commands.ReactivateBusiness;
using Roivo.Application.Tests.Fakes;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Tests.Features.Businesses;

public class ReactivateBusinessHandlerTests
{
    private const string ValidAfm = "094014201";

    private static (ReactivateBusinessHandler handler, FakeBusinessRepository repo, FakeAuditWriter audit) BuildSut(TenantType tenantType = TenantType.Accountant)
    {
        var repo = new FakeBusinessRepository();
        var audit = new FakeAuditWriter();
        var tenant = new FakeTenantContext { CurrentTenantType = tenantType };
        return (new ReactivateBusinessHandler(repo, audit, tenant), repo, audit);
    }

    private static Business SeedInactive(FakeBusinessRepository repo, string name = "Old", string afm = ValidAfm, string? kad = null, string? address = null)
    {
        var b = Business.Create(name, afm, kad, address);
        b.Deactivate();
        repo.Store[b.Id] = b;
        return b;
    }

    [Fact]
    public async Task Happy_path_reactivates_and_preserves_old_details()
    {
        var (handler, repo, audit) = BuildSut();
        var inactive = SeedInactive(repo, "Old Name", ValidAfm, "10.10", "Old Addr");

        var result = await handler.Handle(new ReactivateBusinessCommand(inactive.Id));

        result.Should().BeOfType<ReactivateBusinessResult.Success>();
        var stored = repo.Store[inactive.Id];
        stored.IsActive.Should().BeTrue();
        stored.Name.Should().Be("Old Name");
        stored.Kad.Should().Be("10.10");
        stored.Address.Should().Be("Old Addr");
        audit.Calls.Should().ContainSingle().Which.Action.Should().Be("BusinessReactivated");
    }

    [Fact]
    public async Task Not_found_returns_NotFound()
    {
        var (handler, _, audit) = BuildSut();

        var result = await handler.Handle(new ReactivateBusinessCommand(Guid.NewGuid()));

        result.Should().BeOfType<ReactivateBusinessResult.NotFound>();
        audit.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Already_active_returns_AlreadyActive()
    {
        var (handler, repo, audit) = BuildSut();
        var b = Business.Create("Acme", ValidAfm, null, null);
        repo.Store[b.Id] = b;

        var result = await handler.Handle(new ReactivateBusinessCommand(b.Id));

        result.Should().BeOfType<ReactivateBusinessResult.AlreadyActive>();
        audit.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Conflicts_with_active_returns_ConflictsWithActive()
    {
        var (handler, repo, audit) = BuildSut();
        var inactive = SeedInactive(repo, "Old", ValidAfm);
        // Another business holds the same AFM actively (race condition simulation).
        var racer = Business.Create("Racer", ValidAfm, null, null);
        repo.Store[racer.Id] = racer;

        var result = await handler.Handle(new ReactivateBusinessCommand(inactive.Id));

        result.Should().BeOfType<ReactivateBusinessResult.ConflictsWithActive>()
            .Which.Afm.Should().Be(ValidAfm);
        repo.Store[inactive.Id].IsActive.Should().BeFalse();
        audit.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Forbidden_when_tenant_is_business_type()
    {
        var (handler, repo, audit) = BuildSut(TenantType.Business);
        var inactive = SeedInactive(repo);

        var result = await handler.Handle(new ReactivateBusinessCommand(inactive.Id));

        result.Should().BeOfType<ReactivateBusinessResult.Forbidden>();
        repo.Store[inactive.Id].IsActive.Should().BeFalse();
        audit.Calls.Should().BeEmpty();
    }
}
