using FluentAssertions;
using Roivo.Application.Features.Businesses.Commands.CreateBusiness;
using Roivo.Application.Tests.Fakes;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Tests.Features.Businesses;

public class CreateBusinessHandlerTests
{
    // Well-formed AFMs that pass the Mod 11 check used by AfmValidator.
    private const string ValidAfm = "094014201";
    private const string AnotherValidAfm = "123456783";

    private (CreateBusinessHandler handler, FakeBusinessRepository repo, FakeAuditWriter audit, FakeTenantContext tenant) BuildSut()
    {
        var repo = new FakeBusinessRepository();
        var audit = new FakeAuditWriter();
        var tenant = new FakeTenantContext();
        return (new CreateBusinessHandler(repo, audit, tenant), repo, audit, tenant);
    }

    [Fact]
    public async Task Valid_input_creates_business_and_writes_audit()
    {
        var (handler, repo, audit, tenant) = BuildSut();
        var cmd = new CreateBusinessCommand("Acme AE", ValidAfm, "62.01", "Athens");

        var result = await handler.Handle(cmd);

        var success = result.Should().BeOfType<CreateBusinessResult.Success>().Subject;
        repo.Store.Should().ContainKey(success.BusinessId);
        var stored = repo.Store[success.BusinessId];
        stored.Name.Should().Be("Acme AE");
        stored.Afm.Should().Be(ValidAfm);
        stored.Kad.Should().Be("62.01");
        stored.Address.Should().Be("Athens");
        stored.IsActive.Should().BeTrue();

        var auditCall = audit.Calls.Should().ContainSingle().Subject;
        auditCall.Action.Should().Be("BusinessCreated");
        auditCall.TenantId.Should().Be(tenant.CurrentTenantId);
    }

    [Fact]
    public async Task Invalid_AFM_returns_InvalidAfm_and_writes_nothing()
    {
        var (handler, repo, audit, _) = BuildSut();
        var result = await handler.Handle(new CreateBusinessCommand("Acme", "123", null, null));

        result.Should().BeOfType<CreateBusinessResult.InvalidAfm>();
        repo.Store.Should().BeEmpty();
        audit.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Active_duplicate_returns_ActiveDuplicate_and_does_not_add()
    {
        var (handler, repo, audit, _) = BuildSut();
        repo.Store[Guid.NewGuid()] = new Business { Name = "Existing", Afm = ValidAfm, IsActive = true };

        var result = await handler.Handle(new CreateBusinessCommand("New", ValidAfm, null, null));

        result.Should().BeOfType<CreateBusinessResult.ActiveDuplicate>()
            .Which.Afm.Should().Be(ValidAfm);
        repo.Store.Should().HaveCount(1);
        audit.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Inactive_duplicate_returns_InactiveDuplicate_with_old_details()
    {
        var (handler, repo, audit, _) = BuildSut();
        var existingId = Guid.NewGuid();
        repo.Store[existingId] = new Business
        {
            Id = existingId,
            Name = "Old Name",
            Afm = ValidAfm,
            Address = "Old Address",
            IsActive = false
        };

        var result = await handler.Handle(new CreateBusinessCommand("New Name", ValidAfm, null, null));

        var inactive = result.Should().BeOfType<CreateBusinessResult.InactiveDuplicate>().Subject;
        inactive.ExistingId.Should().Be(existingId);
        inactive.OldName.Should().Be("Old Name");
        inactive.OldAddress.Should().Be("Old Address");
        repo.Store.Should().HaveCount(1);
        audit.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Trims_whitespace_from_name_and_AFM()
    {
        var (handler, repo, _, _) = BuildSut();

        var result = await handler.Handle(new CreateBusinessCommand("  Acme  ", $"  {ValidAfm}  ", null, null));

        var success = result.Should().BeOfType<CreateBusinessResult.Success>().Subject;
        var stored = repo.Store[success.BusinessId];
        stored.Name.Should().Be("Acme");
        stored.Afm.Should().Be(ValidAfm);
    }

    [Fact]
    public async Task Empty_kad_and_address_stored_as_null()
    {
        var (handler, repo, _, _) = BuildSut();

        var result = await handler.Handle(new CreateBusinessCommand("Acme", ValidAfm, "   ", ""));

        var success = result.Should().BeOfType<CreateBusinessResult.Success>().Subject;
        var stored = repo.Store[success.BusinessId];
        stored.Kad.Should().BeNull();
        stored.Address.Should().BeNull();
    }

    [Fact]
    public async Task Different_AFM_does_not_collide_with_existing()
    {
        var (handler, repo, _, _) = BuildSut();
        repo.Store[Guid.NewGuid()] = new Business { Name = "Other", Afm = AnotherValidAfm, IsActive = true };

        var result = await handler.Handle(new CreateBusinessCommand("Acme", ValidAfm, null, null));

        result.Should().BeOfType<CreateBusinessResult.Success>();
        repo.Store.Should().HaveCount(2);
    }
}
