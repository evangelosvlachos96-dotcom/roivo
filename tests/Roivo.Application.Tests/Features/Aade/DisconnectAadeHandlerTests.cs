using FluentAssertions;
using Roivo.Application.Features.Aade.Commands.DisconnectAade;
using Roivo.Application.Tests.Fakes;
using Roivo.Core.Domain.Auditing;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Tests.Features.Aade;

public class DisconnectAadeHandlerTests
{
    private const string ValidAfm = "094014201";

    private static (DisconnectAadeHandler handler, FakeBusinessRepository businesses, FakeAadeCredentialStore store, FakeAuditWriter audit) BuildSut()
    {
        var businesses = new FakeBusinessRepository();
        var store = new FakeAadeCredentialStore();
        var audit = new FakeAuditWriter();
        var tenant = new FakeTenantContext();
        return (new DisconnectAadeHandler(businesses, store, audit, tenant), businesses, store, audit);
    }

    [Fact]
    public async Task Success_clears_credentials_and_audits()
    {
        var (handler, businesses, store, audit) = BuildSut();
        var b = Business.Create("Acme", ValidAfm, null, null);
        businesses.Store[b.Id] = b;
        store.Store[b.Id] = new Roivo.Application.Abstractions.Aade.AadeCredentials("u", "k");

        var result = await handler.Handle(new DisconnectAadeCommand(b.Id));

        result.Should().BeOfType<DisconnectAadeResult.Success>();
        store.Store.Should().NotContainKey(b.Id);
        audit.Calls.Should().ContainSingle().Which.Action.Should().Be(AuditAction.AadeDisconnected);
    }

    [Fact]
    public async Task BusinessNotFound_when_missing()
    {
        var (handler, _, _, audit) = BuildSut();

        var result = await handler.Handle(new DisconnectAadeCommand(Guid.NewGuid()));

        result.Should().BeOfType<DisconnectAadeResult.BusinessNotFound>();
        audit.Calls.Should().BeEmpty();
    }
}
