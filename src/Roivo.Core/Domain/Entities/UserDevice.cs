using Roivo.Core.Domain.Exceptions;

namespace Roivo.Core.Domain.Entities;

/// <summary>
/// A device a user has signed in from, remembered so a sign-in from somewhere
/// new can be flagged to them.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately not <c>ITenantScoped</c>. It hangs off the Identity user, and
/// the global tenant filter would hide it during sign-in — which is the one
/// moment it has to be readable, before any tenant claim exists.
/// </para>
/// <para>
/// What is stored is a hash, never the raw User-Agent or a full IP address.
/// The point is only to answer "have we seen this before", and that question
/// does not need either in the clear.
/// </para>
/// </remarks>
public class UserDevice
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid UserId { get; private set; }

    /// <summary>
    /// SHA-256 of the User-Agent plus the network part of the address. Keeps
    /// the check stable while the user stays on one network and one browser.
    /// </summary>
    public string DeviceHash { get; private set; } = null!;

    /// <summary>Something a person can recognise, e.g. "Chrome on Windows".</summary>
    public string DeviceName { get; private set; } = null!;

    /// <summary>
    /// The network part of the address only — the last octet is dropped. Enough
    /// for the owner to recognise "this was my office", not enough to track.
    /// </summary>
    public string IpPrefix { get; private set; } = null!;

    public DateTime FirstSeenAt { get; private set; } = DateTime.UtcNow;

    public DateTime LastSeenAt { get; private set; } = DateTime.UtcNow;

    private UserDevice() { }

    public static UserDevice Create(Guid userId, string deviceHash, string deviceName, string ipPrefix)
    {
        if (userId == Guid.Empty)
            throw new DomainException("A device must belong to a user.");

        ArgumentException.ThrowIfNullOrWhiteSpace(deviceHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceName);

        var now = DateTime.UtcNow;

        return new UserDevice
        {
            UserId = userId,
            DeviceHash = deviceHash,
            DeviceName = deviceName.Trim(),
            IpPrefix = (ipPrefix ?? "").Trim(),
            FirstSeenAt = now,
            LastSeenAt = now,
        };
    }

    public void Touch(DateTime seenAtUtc) => LastSeenAt = seenAtUtc;
}
