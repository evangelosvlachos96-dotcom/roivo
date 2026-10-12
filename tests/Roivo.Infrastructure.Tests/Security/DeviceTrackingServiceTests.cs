using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Enums;
using Roivo.Infrastructure.Persistence;
using Roivo.Infrastructure.Security;

namespace Roivo.Infrastructure.Tests.Security;

/// <summary>
/// Covers what the tracker promises: that a device is recognised again, that a
/// different browser or a different network is not, that the notice retires
/// once acknowledged, and that one account cannot remove another's device.
/// </summary>
public sealed class DeviceTrackingServiceTests : IDisposable
{
    private const string Chrome =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0 Safari/537.36";

    private const string Firefox =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:130.0) Gecko/20100101 Firefox/130.0";

    private const string Office = "203.0.113.47";
    private const string Home = "198.51.100.9";

    private static CancellationToken TestCancellation => CancellationToken.None;

    private readonly ServiceProvider _services;
    private readonly IDbContextFactory<ApplicationDbContext> _factory;
    private readonly DeviceTrackingService _sut;

    public DeviceTrackingServiceTests()
    {
        _services = new ServiceCollection()
            .AddSingleton<ITenantContext>(new StubTenantContext())
            .AddDbContextFactory<ApplicationDbContext>(
                options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()))
            .BuildServiceProvider();

        _factory = _services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        _sut = new DeviceTrackingService(_factory);
    }

    // ------------------------------------------------------------ recognition

    [Fact]
    public async Task IsKnownDeviceAsync_IsFalseBeforeTheDeviceHasBeenSeen()
    {
        var user = Guid.NewGuid();

        (await _sut.IsKnownDeviceAsync(user, Chrome, Office, TestCancellation))
            .Should().BeFalse();
    }

    [Fact]
    public async Task IsKnownDeviceAsync_IsTrueAfterRegistering()
    {
        var user = Guid.NewGuid();
        await _sut.RegisterDeviceAsync(user, Chrome, Office, TestCancellation);

        (await _sut.IsKnownDeviceAsync(user, Chrome, Office, TestCancellation))
            .Should().BeTrue();
    }

    [Fact]
    public async Task IsKnownDeviceAsync_IgnoresTheHostPartOfTheAddress()
    {
        var user = Guid.NewGuid();
        await _sut.RegisterDeviceAsync(user, Chrome, "203.0.113.47", TestCancellation);

        // Same network, different lease. A DHCP renewal must not read as a new
        // device, or the warning would fire every few days for nobody.
        (await _sut.IsKnownDeviceAsync(user, Chrome, "203.0.113.200", TestCancellation))
            .Should().BeTrue();
    }

    [Fact]
    public async Task IsKnownDeviceAsync_IsFalseForADifferentBrowser()
    {
        var user = Guid.NewGuid();
        await _sut.RegisterDeviceAsync(user, Chrome, Office, TestCancellation);

        (await _sut.IsKnownDeviceAsync(user, Firefox, Office, TestCancellation))
            .Should().BeFalse();
    }

    [Fact]
    public async Task IsKnownDeviceAsync_IsFalseFromADifferentNetwork()
    {
        var user = Guid.NewGuid();
        await _sut.RegisterDeviceAsync(user, Chrome, Office, TestCancellation);

        (await _sut.IsKnownDeviceAsync(user, Chrome, Home, TestCancellation))
            .Should().BeFalse();
    }

    [Fact]
    public async Task IsKnownDeviceAsync_IsFalseForAnotherUserOnTheSameMachine()
    {
        var mine = Guid.NewGuid();
        var theirs = Guid.NewGuid();
        await _sut.RegisterDeviceAsync(mine, Chrome, Office, TestCancellation);

        (await _sut.IsKnownDeviceAsync(theirs, Chrome, Office, TestCancellation))
            .Should().BeFalse();
    }

    // ------------------------------------------------------------ registering

    [Fact]
    public async Task RegisterDeviceAsync_StoresNeitherTheUserAgentNorTheFullAddress()
    {
        var user = Guid.NewGuid();
        await _sut.RegisterDeviceAsync(user, Chrome, Office, TestCancellation);

        await using var db = await _factory.CreateDbContextAsync(TestCancellation);
        var device = await db.UserDevices.SingleAsync(TestCancellation);

        device.DeviceHash.Should().NotContain("Mozilla");
        device.IpPrefix.Should().Be("203.0.113");
        device.IpPrefix.Should().NotBe(Office);
    }

    [Fact]
    public async Task RegisterDeviceAsync_DoesNotDuplicateADeviceAlreadyKnown()
    {
        var user = Guid.NewGuid();
        var first = await _sut.RegisterDeviceAsync(user, Chrome, Office, TestCancellation);
        var second = await _sut.RegisterDeviceAsync(user, Chrome, Office, TestCancellation);

        second.Id.Should().Be(first.Id);
        (await _sut.ListDevicesAsync(user, TestCancellation)).Should().HaveCount(1);
    }

    [Fact]
    public async Task RegisterDeviceAsync_NamesTheDeviceFromTheUserAgent()
    {
        var user = Guid.NewGuid();

        (await _sut.RegisterDeviceAsync(user, Chrome, Office, TestCancellation))
            .DeviceName.Should().Be("Chrome on Windows");
    }

    [Theory]
    // Edge and Opera both carry "Chrome", and Chrome carries "Safari", so the
    // order these are tested in is the whole behaviour.
    [InlineData("Mozilla/5.0 (Windows NT 10.0) Chrome/140.0 Safari/537.36 Edg/140.0", "Edge on Windows")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0) Chrome/140.0 Safari/537.36 OPR/118.0", "Opera on Windows")]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) Version/18.0 Safari/605.1", "Safari on macOS")]
    // iPadOS reports itself as Macintosh, so iPad has to be tested first.
    [InlineData("Mozilla/5.0 (iPad; CPU OS 18_0 like Mac OS X) Version/18.0 Safari/604.1", "Safari on iPad")]
    [InlineData("", "Unknown device")]
    public void DescribeDevice_PrefersTheMoreSpecificName(string userAgent, string expected) =>
        DeviceTrackingService.DescribeDevice(userAgent).Should().Be(expected);

    // --------------------------------------------------------- the new notice

    [Fact]
    public async Task GetUnacknowledgedDeviceAsync_ReturnsADeviceJustRegistered()
    {
        var user = Guid.NewGuid();
        var registered = await _sut.RegisterDeviceAsync(user, Chrome, Office, TestCancellation);

        var pending = await _sut.GetUnacknowledgedDeviceAsync(
            user, TimeSpan.FromMinutes(10), TestCancellation);

        pending!.Id.Should().Be(registered.Id);
    }

    [Fact]
    public async Task GetUnacknowledgedDeviceAsync_IsSilentOnceAcknowledged()
    {
        var user = Guid.NewGuid();
        var registered = await _sut.RegisterDeviceAsync(user, Chrome, Office, TestCancellation);

        await _sut.AcknowledgeDeviceAsync(user, registered.Id, TestCancellation);

        (await _sut.GetUnacknowledgedDeviceAsync(user, TimeSpan.FromMinutes(10), TestCancellation))
            .Should().BeNull();
    }

    [Fact]
    public async Task GetUnacknowledgedDeviceAsync_IsSilentOutsideTheWindow()
    {
        var user = Guid.NewGuid();
        await _sut.RegisterDeviceAsync(user, Chrome, Office, TestCancellation);

        // A window that closed before the sign-in happened: the notice is about
        // the session the user just started, not one from last week.
        (await _sut.GetUnacknowledgedDeviceAsync(user, TimeSpan.Zero, TestCancellation))
            .Should().BeNull();
    }

    [Fact]
    public async Task AcknowledgeDeviceAsync_IgnoresAnotherUsersDevice()
    {
        var mine = Guid.NewGuid();
        var theirs = Guid.NewGuid();
        var device = await _sut.RegisterDeviceAsync(mine, Chrome, Office, TestCancellation);

        await _sut.AcknowledgeDeviceAsync(theirs, device.Id, TestCancellation);

        (await _sut.GetUnacknowledgedDeviceAsync(mine, TimeSpan.FromMinutes(10), TestCancellation))
            .Should().NotBeNull();
    }

    // ----------------------------------------------------------------- removal

    [Fact]
    public async Task RemoveDeviceAsync_RemovesTheUsersOwnDevice()
    {
        var user = Guid.NewGuid();
        var device = await _sut.RegisterDeviceAsync(user, Chrome, Office, TestCancellation);

        (await _sut.RemoveDeviceAsync(user, device.Id, TestCancellation)).Should().BeTrue();
        (await _sut.ListDevicesAsync(user, TestCancellation)).Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveDeviceAsync_WillNotRemoveAnotherUsersDevice()
    {
        var mine = Guid.NewGuid();
        var theirs = Guid.NewGuid();
        var device = await _sut.RegisterDeviceAsync(mine, Chrome, Office, TestCancellation);

        (await _sut.RemoveDeviceAsync(theirs, device.Id, TestCancellation)).Should().BeFalse();
        (await _sut.ListDevicesAsync(mine, TestCancellation)).Should().HaveCount(1);
    }

    [Fact]
    public async Task ListDevicesAsync_ShowsOnlyTheUsersOwnDevices()
    {
        var mine = Guid.NewGuid();
        var theirs = Guid.NewGuid();

        await _sut.RegisterDeviceAsync(mine, Chrome, Office, TestCancellation);
        await _sut.RegisterDeviceAsync(mine, Firefox, Office, TestCancellation);
        await _sut.RegisterDeviceAsync(theirs, Chrome, Home, TestCancellation);

        (await _sut.ListDevicesAsync(mine, TestCancellation)).Should().HaveCount(2);
    }

    public void Dispose() => _services.Dispose();

    /// <summary>
    /// UserDevice is deliberately outside the tenant filter, but the context
    /// still needs a tenant to build; nothing here depends on which one.
    /// </summary>
    private sealed class StubTenantContext : ITenantContext
    {
        public Guid CurrentTenantId { get; } = Guid.NewGuid();
        public TenantType CurrentTenantType => TenantType.Business;
    }
}
