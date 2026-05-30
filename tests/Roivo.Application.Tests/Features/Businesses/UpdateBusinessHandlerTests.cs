using FluentAssertions;
using Roivo.Application.Features.Businesses.Commands.UpdateBusiness;
using Roivo.Application.Tests.Fakes;
using Roivo.Core.Domain.Auditing;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Tests.Features.Businesses;

public class UpdateBusinessHandlerTests
{
    private const string ValidAfm = "094014201";
    private const string AnotherValidAfm = "123456783";

    private static (UpdateBusinessHandler handler, FakeBusinessRepository repo, FakeAuditWriter audit) BuildSut(TenantType tenantType = TenantType.Accountant)
    {
        var repo = new FakeBusinessRepository();
        var audit = new FakeAuditWriter();
        var tenant = new FakeTenantContext { CurrentTenantType = tenantType };
        return (new UpdateBusinessHandler(repo, audit, tenant), repo, audit);
    }

    private static Business SeedActive(FakeBusinessRepository repo, string name = "Acme", string afm = ValidAfm, string? kad = null, string? address = null)
    {
        var b = Business.Create(name, afm, kad, address);
        repo.Store[b.Id] = b;
        return b;
    }

    [Fact]
    public async Task Happy_path_updates_fields_and_writes_audit_with_diff()
    {
        var (handler, repo, audit) = BuildSut();
        var existing = SeedActive(repo, "Old", ValidAfm, "10.10", "Old Addr");

        var result = await handler.Handle(new UpdateBusinessCommand(existing.Id, "New", ValidAfm, "20.20", "New Addr"));

        result.Should().BeOfType<UpdateBusinessResult.Success>();
        var stored = repo.Store[existing.Id];
        stored.Name.Should().Be("New");
        stored.Kad.Should().Be("20.20");
        stored.Address.Should().Be("New Addr");

        var call = audit.Calls.Should().ContainSingle().Subject;
        call.Action.Should().Be(AuditAction.BusinessUpdated);
        var details = call.Details.Should().BeAssignableTo<IDictionary<string, object?>>().Subject;
        details.Should().ContainKeys("Name", "Kad", "Address");
        details.Should().NotContainKey("Afm");
    }

    [Fact]
    public async Task Not_found_returns_NotFound()
    {
        var (handler, _, audit) = BuildSut();

        var result = await handler.Handle(new UpdateBusinessCommand(Guid.NewGuid(), "X", ValidAfm, null, null));

        result.Should().BeOfType<UpdateBusinessResult.NotFound>();
        audit.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Invalid_AFM_returns_InvalidAfm()
    {
        var (handler, repo, audit) = BuildSut();
        var existing = SeedActive(repo);

        var result = await handler.Handle(new UpdateBusinessCommand(existing.Id, "X", "bad", null, null));

        result.Should().BeOfType<UpdateBusinessResult.InvalidAfm>();
        repo.Store[existing.Id].Name.Should().Be("Acme");
        audit.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Active_duplicate_of_another_business_returns_ActiveDuplicateAfm()
    {
        var (handler, repo, audit) = BuildSut();
        var target = SeedActive(repo, "Target", ValidAfm);
        SeedActive(repo, "Other", AnotherValidAfm);

        var result = await handler.Handle(new UpdateBusinessCommand(target.Id, "Target", AnotherValidAfm, null, null));

        result.Should().BeOfType<UpdateBusinessResult.ActiveDuplicateAfm>()
            .Which.Afm.Should().Be(AnotherValidAfm);
        repo.Store[target.Id].Afm.Should().Be(ValidAfm);
        audit.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task No_changes_returns_Success_without_audit()
    {
        var (handler, repo, audit) = BuildSut();
        var existing = SeedActive(repo, "Acme", ValidAfm, "10.10", "Addr");

        var result = await handler.Handle(new UpdateBusinessCommand(existing.Id, "Acme", ValidAfm, "10.10", "Addr"));

        result.Should().BeOfType<UpdateBusinessResult.Success>();
        audit.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Audit_details_capture_only_changed_fields()
    {
        var (handler, repo, audit) = BuildSut();
        var existing = SeedActive(repo, "Acme", ValidAfm, "10.10", "Addr");

        // Only address changes.
        var result = await handler.Handle(new UpdateBusinessCommand(existing.Id, "Acme", ValidAfm, "10.10", "New Addr"));

        result.Should().BeOfType<UpdateBusinessResult.Success>();
        var details = audit.Calls.Single().Details.Should().BeAssignableTo<IDictionary<string, object?>>().Subject;
        details.Should().ContainKey("Address");
        details.Should().NotContainKeys("Name", "Afm", "Kad");
    }

    [Fact]
    public async Task Forbidden_when_business_tenant_tries_to_change_afm()
    {
        var (handler, repo, audit) = BuildSut(TenantType.Business);
        var existing = SeedActive(repo, "Acme", ValidAfm);

        var result = await handler.Handle(new UpdateBusinessCommand(existing.Id, "Acme", AnotherValidAfm, null, null));

        result.Should().BeOfType<UpdateBusinessResult.Forbidden>();
        repo.Store[existing.Id].Afm.Should().Be(ValidAfm);
        audit.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Succeeds_when_business_tenant_changes_name_only()
    {
        var (handler, repo, audit) = BuildSut(TenantType.Business);
        var existing = SeedActive(repo, "Old Name", ValidAfm);

        var result = await handler.Handle(new UpdateBusinessCommand(existing.Id, "New Name", ValidAfm, null, null));

        result.Should().BeOfType<UpdateBusinessResult.Success>();
        repo.Store[existing.Id].Name.Should().Be("New Name");
        audit.Calls.Should().ContainSingle();
    }
}
