namespace Roivo.Application.Abstractions.Security;

/// <summary>
/// Remembers which devices a user signs in from, so a sign-in from a new one
/// can be surfaced to them.
/// </summary>
/// <remarks>
/// Identifies a device by hashing the User-Agent together with the network part
/// of the caller's address. That is a heuristic, not an identity: a new browser,
/// a browser upgrade, or a different network all read as new. It is tuned to
/// err that way — a spurious "new sign-in" notice is a small annoyance, a
/// missed one is the thing this exists to prevent.
/// </remarks>
public interface IDeviceTrackingService
{
    /// <summary>
    /// True when this user has signed in from this device before. Touches the
    /// last-seen stamp when it has.
    /// </summary>
    Task<bool> IsKnownDeviceAsync(
        Guid userId, string? userAgent, string? ipAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records the device. Safe to call for one already known — it updates
    /// rather than duplicating.
    /// </summary>
    Task<UserDeviceView> RegisterDeviceAsync(
        Guid userId, string? userAgent, string? ipAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// The device registered for this user within <paramref name="within"/> and
    /// not used since, or null.
    /// </summary>
    /// <remarks>
    /// This is how the Blazor shell learns about a new sign-in. The login page
    /// is a Razor Page, and neither TempData nor HttpContext.Items survives the
    /// redirect into a Blazor circuit, so the shell asks the tracker rather
    /// than having the state handed to it. Self-contained, and it cannot be
    /// broken by a change to how login redirects.
    /// </remarks>
    Task<UserDeviceView?> GetUnacknowledgedDeviceAsync(
        Guid userId, TimeSpan within, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the new-device notice as seen, so it stops being returned by
    /// <see cref="GetUnacknowledgedDeviceAsync"/>.
    /// </summary>
    Task AcknowledgeDeviceAsync(
        Guid userId, Guid deviceId, CancellationToken cancellationToken = default);

    /// <summary>Everything known for this user, most recently seen first.</summary>
    Task<IReadOnlyList<UserDeviceView>> ListDevicesAsync(
        Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Forgets a device. Scoped to the user so one account cannot remove
    /// another's; a device that is not theirs is simply not found.
    /// </summary>
    Task<bool> RemoveDeviceAsync(
        Guid userId, Guid deviceId, CancellationToken cancellationToken = default);
}

/// <summary>One device, as the settings page shows it.</summary>
public sealed record UserDeviceView(
    Guid Id,
    string DeviceName,
    string IpPrefix,
    DateTime FirstSeenAt,
    DateTime LastSeenAt);
