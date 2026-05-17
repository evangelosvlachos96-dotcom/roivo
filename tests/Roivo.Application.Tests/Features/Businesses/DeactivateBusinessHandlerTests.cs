using FluentAssertions;
using Roivo.Application.Features.Businesses.Commands.DeactivateBusiness;
using Roivo.Application.Tests.Fakes;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Tests.Features.Businesses;

public class DeactivateBusinessHandlerTests
{
    private const string ValidAfm = "094014201";

    private (DeactivateBusinessHandler handler, FakeBusinessRepository repo, FakeAuditWriter audit) BuildSut()
    {
        var repo = new FakeBusinessRepository();
        var audit = new FakeAuditWriter();
        return (new DeactivateBusinessHandler(repo, audit, new FakeTenantContext()), repo, audit);
    }

    [Fact]
    public async Task Happy_path_flips_IsActive_and_writes_audit()
    {
        var (handler, repo, audit) = BuildSut();
        var id = Guid.NewGuid();
        repo.Store[id] = new Business { Id = id, Name = "Acme", Afm = ValidAfm, IsActive = true };

        var result = await handler.Handle(new DeactivateBusinessCommand(id));

        result.Should().BeOfType<DeactivateBusinessResult.Success>();
        repo.Store[id].IsActive.Should().BeFalse();
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
        var id = Guid.NewGuid();
        repo.Store[id] = new Business { Id = id, Name = "Acme", Afm = ValidAfm, IsActive = false };

        var result = await handler.Handle(new DeactivateBusinessCommand(id));

        result.Should().BeOfType<DeactivateBusinessResult.AlreadyInactive>();
        audit.Calls.Should().BeEmpty();
    }
}
