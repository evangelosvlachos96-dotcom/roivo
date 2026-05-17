using FluentAssertions;
using Roivo.Application.Features.Businesses.Commands.ReactivateBusiness;
using Roivo.Application.Tests.Fakes;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Tests.Features.Businesses;

public class ReactivateBusinessHandlerTests
{
    private const string ValidAfm = "094014201";

    private (ReactivateBusinessHandler handler, FakeBusinessRepository repo, FakeAuditWriter audit) BuildSut()
    {
        var repo = new FakeBusinessRepository();
        var audit = new FakeAuditWriter();
        return (new ReactivateBusinessHandler(repo, audit, new FakeTenantContext()), repo, audit);
    }

    [Fact]
    public async Task Happy_path_reactivates_and_applies_new_details()
    {
        var (handler, repo, audit) = BuildSut();
        var id = Guid.NewGuid();
        repo.Store[id] = new Business { Id = id, Name = "Old", Afm = ValidAfm, Address = "Old Addr", IsActive = false };

        var result = await handler.Handle(new ReactivateBusinessCommand(id, "New", "30.30", "New Addr"));

        result.Should().BeOfType<ReactivateBusinessResult.Success>();
        var stored = repo.Store[id];
        stored.IsActive.Should().BeTrue();
        stored.Name.Should().Be("New");
        stored.Kad.Should().Be("30.30");
        stored.Address.Should().Be("New Addr");
        audit.Calls.Should().ContainSingle().Which.Action.Should().Be("BusinessReactivated");
    }

    [Fact]
    public async Task Not_found_returns_NotFound()
    {
        var (handler, _, audit) = BuildSut();

        var result = await handler.Handle(new ReactivateBusinessCommand(Guid.NewGuid(), "X", null, null));

        result.Should().BeOfType<ReactivateBusinessResult.NotFound>();
        audit.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Already_active_returns_AlreadyActive()
    {
        var (handler, repo, audit) = BuildSut();
        var id = Guid.NewGuid();
        repo.Store[id] = new Business { Id = id, Name = "Acme", Afm = ValidAfm, IsActive = true };

        var result = await handler.Handle(new ReactivateBusinessCommand(id, "X", null, null));

        result.Should().BeOfType<ReactivateBusinessResult.AlreadyActive>();
        audit.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Conflicts_with_active_returns_ConflictsWithActive()
    {
        var (handler, repo, audit) = BuildSut();
        var inactiveId = Guid.NewGuid();
        repo.Store[inactiveId] = new Business { Id = inactiveId, Name = "Old", Afm = ValidAfm, IsActive = false };
        // Another business holds the same AFM actively (race condition simulation).
        var activeId = Guid.NewGuid();
        repo.Store[activeId] = new Business { Id = activeId, Name = "Racer", Afm = ValidAfm, IsActive = true };

        var result = await handler.Handle(new ReactivateBusinessCommand(inactiveId, "New", null, null));

        result.Should().BeOfType<ReactivateBusinessResult.ConflictsWithActive>()
            .Which.Afm.Should().Be(ValidAfm);
        repo.Store[inactiveId].IsActive.Should().BeFalse();
        audit.Calls.Should().BeEmpty();
    }
}
