using FluentAssertions;
using Roivo.Application.Abstractions.Aade;
using Roivo.Application.Features.Aade.Queries.GetAadeConnectionStatus;
using Roivo.Application.Tests.Fakes;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Tests.Features.Aade;

public class GetAadeConnectionStatusHandlerTests
{
    private const string ValidAfm = "094014201";

    [Fact]
    public async Task Connected_returns_masked_user_and_last_sync()
    {
        var businesses = new FakeBusinessRepository();
        var store = new FakeAadeCredentialStore();
        var b = Business.Create("Acme", ValidAfm, null, null);
        b.RecordAadeSync(new DateTime(2026, 5, 1, 3, 0, 0, DateTimeKind.Utc));
        businesses.Store[b.Id] = b;
        store.Store[b.Id] = new AadeCredentials("aade-user-1234567", "subkey");
        var handler = new GetAadeConnectionStatusHandler(businesses, store);

        var info = await handler.Handle(new GetAadeConnectionStatusQuery(b.Id));

        info.IsConnected.Should().BeTrue();
        info.LastSyncAt.Should().Be(new DateTime(2026, 5, 1, 3, 0, 0, DateTimeKind.Utc));
        info.MaskedUserId.Should().Be("aade***");
    }

    [Fact]
    public async Task NotConnected_when_no_credentials()
    {
        var businesses = new FakeBusinessRepository();
        var store = new FakeAadeCredentialStore();
        var b = Business.Create("Acme", ValidAfm, null, null);
        businesses.Store[b.Id] = b;
        var handler = new GetAadeConnectionStatusHandler(businesses, store);

        var info = await handler.Handle(new GetAadeConnectionStatusQuery(b.Id));

        info.IsConnected.Should().BeFalse();
        info.MaskedUserId.Should().BeNull();
    }

    [Fact]
    public async Task NotConnected_when_business_missing()
    {
        var businesses = new FakeBusinessRepository();
        var store = new FakeAadeCredentialStore();
        var handler = new GetAadeConnectionStatusHandler(businesses, store);

        var info = await handler.Handle(new GetAadeConnectionStatusQuery(Guid.NewGuid()));

        info.IsConnected.Should().BeFalse();
        info.LastSyncAt.Should().BeNull();
    }
}
