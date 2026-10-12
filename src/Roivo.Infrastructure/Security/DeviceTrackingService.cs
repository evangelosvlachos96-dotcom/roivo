using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Roivo.Application.Abstractions.Security;
using Roivo.Core.Domain.Entities;
using Roivo.Infrastructure.Persistence;

namespace Roivo.Infrastructure.Security;

/// <inheritdoc />
public sealed class DeviceTrackingService : IDeviceTrackingService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public DeviceTrackingService(IDbContextFactory<ApplicationDbContext> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    public async Task<bool> IsKnownDeviceAsync(
        Guid userId, string? userAgent, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty) return false;

        var hash = Fingerprint(userAgent, ipAddress);

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        var device = await db.UserDevices
            .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceHash == hash, cancellationToken);

        if (device is null) return false;

        // Seeing it again is the whole evidence that it is still in use.
        device.Touch(DateTime.UtcNow);
        await db.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<UserDeviceView> RegisterDeviceAsync(
        Guid userId, string? userAgent, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var hash = Fingerprint(userAgent, ipAddress);
        var name = DescribeDevice(userAgent);
        var prefix = NetworkPrefix(ipAddress);

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        // Two sign-ins racing on the same new device would otherwise insert the
        // same fingerprint twice and notify the user twice.
        var existing = await db.UserDevices
            .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceHash == hash, cancellationToken);

        if (existing is not null)
        {
            existing.Touch(DateTime.UtcNow);
            await db.SaveChangesAsync(cancellationToken);
            return ToView(existing);
        }

        var device = UserDevice.Create(userId, hash, name, prefix);
        db.UserDevices.Add(device);
        await db.SaveChangesAsync(cancellationToken);

        return ToView(device);
    }

    public async Task<UserDeviceView?> GetUnacknowledgedDeviceAsync(
        Guid userId, TimeSpan within, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow - within;

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        // FirstSeenAt == LastSeenAt is the tell: the device has been registered
        // but not touched since, so the owner has not acknowledged the notice.
        var device = await db.UserDevices
            .AsNoTracking()
            .Where(d => d.UserId == userId
                && d.FirstSeenAt >= cutoff
                && d.LastSeenAt == d.FirstSeenAt)
            .OrderByDescending(d => d.FirstSeenAt)
            .FirstOrDefaultAsync(cancellationToken);

        return device is null ? null : ToView(device);
    }

    public async Task AcknowledgeDeviceAsync(
        Guid userId, Guid deviceId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        var device = await db.UserDevices
            .FirstOrDefaultAsync(d => d.Id == deviceId && d.UserId == userId, cancellationToken);

        if (device is null) return;

        // Moving LastSeenAt past FirstSeenAt is what retires the notice.
        device.Touch(DateTime.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserDeviceView>> ListDevicesAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        return await db.UserDevices
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.LastSeenAt)
            .Select(d => new UserDeviceView(d.Id, d.DeviceName, d.IpPrefix, d.FirstSeenAt, d.LastSeenAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> RemoveDeviceAsync(
        Guid userId, Guid deviceId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        // The user id is part of the predicate, not a check afterwards: one
        // account must never be able to remove another's device by guessing an
        // id. A device that is not theirs simply does not come back.
        var device = await db.UserDevices
            .FirstOrDefaultAsync(d => d.Id == deviceId && d.UserId == userId, cancellationToken);

        if (device is null) return false;

        db.UserDevices.Remove(device);
        await db.SaveChangesAsync(cancellationToken);

        return true;
    }

    // ---------------------------------------------------------- fingerprint

    /// <summary>
    /// A stable handle for "this browser on this network". Hashed because the
    /// question is only ever equality, and neither input needs storing in the
    /// clear to answer it.
    /// </summary>
    private static string Fingerprint(string? userAgent, string? ipAddress)
    {
        var material = $"{(userAgent ?? "").Trim()}|{NetworkPrefix(ipAddress)}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(material));

        return Convert.ToHexStringLower(bytes);
    }

    /// <summary>
    /// The network part of an address: the first three octets of IPv4, or the
    /// first four groups of IPv6. Dropping the host part keeps the fingerprint
    /// stable across DHCP renewals and stores less than we were given.
    /// </summary>
    private static string NetworkPrefix(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress)) return "";

        // A proxied address can arrive with a port, or as an IPv4-mapped IPv6.
        var trimmed = ipAddress.Trim();

        if (!IPAddress.TryParse(trimmed, out var parsed))
            return "";

        if (parsed.IsIPv4MappedToIPv6)
            parsed = parsed.MapToIPv4();

        var text = parsed.ToString();

        if (parsed.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var octets = text.Split('.');
            return octets.Length == 4 ? string.Join('.', octets.Take(3)) : text;
        }

        var groups = text.Split(':');
        return groups.Length >= 4 ? string.Join(':', groups.Take(4)) : text;
    }

    /// <summary>
    /// A human-readable name from the User-Agent. Deliberately crude: this is a
    /// label in a list the owner reads, not analytics, and a full UA parser
    /// would be a dependency and a maintenance burden for no added safety.
    /// </summary>
    public static string DescribeDevice(string? userAgent)
    {
        var ua = userAgent ?? "";

        // Order matters. Edge and Opera both carry "Chrome", and Chrome carries
        // "Safari", so the more specific names have to be tested first.
        var browser =
            Contains(ua, "Edg") ? "Edge"
            : Contains(ua, "OPR") || Contains(ua, "Opera") ? "Opera"
            : Contains(ua, "Firefox") ? "Firefox"
            : Contains(ua, "Chrome") ? "Chrome"
            : Contains(ua, "Safari") ? "Safari"
            : null;

        // iPadOS reports itself as Macintosh, so iPad is tested before macOS.
        var platform =
            Contains(ua, "iPhone") ? "iPhone"
            : Contains(ua, "iPad") ? "iPad"
            : Contains(ua, "Android") ? "Android"
            : Contains(ua, "Windows") ? "Windows"
            : Contains(ua, "Mac OS") || Contains(ua, "Macintosh") ? "macOS"
            : Contains(ua, "Linux") ? "Linux"
            : null;

        return (browser, platform) switch
        {
            (null, null) => "Unknown device",
            (not null, null) => browser,
            (null, not null) => platform,
            _ => string.Create(CultureInfo.InvariantCulture, $"{browser} on {platform}"),
        };
    }

    private static bool Contains(string haystack, string needle) =>
        haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);

    private static UserDeviceView ToView(UserDevice device) =>
        new(device.Id, device.DeviceName, device.IpPrefix, device.FirstSeenAt, device.LastSeenAt);
}
