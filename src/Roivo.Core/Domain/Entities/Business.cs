using Roivo.Core.Domain.Exceptions;
using Roivo.Core.Domain.Interfaces;
using Roivo.Core.Domain.Validation;

namespace Roivo.Core.Domain.Entities;

public class Business : ITenantScoped
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    // Set automatically by ApplicationDbContext.SaveChangesAsync override.
    // Public setter is required because the override sets it post-construction.
    public Guid TenantId { get; set; }

    public string Name { get; private set; } = null!;
    public string Afm { get; private set; } = null!;
    public string? Kad { get; private set; }
    public string? Address { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // AADE credentials are written exclusively through IAadeCredentialStore,
    // which encrypts before storing. Both columns hold cipher-text — never
    // plaintext. UI/handlers go through the store, never read these directly.
    public string? AadeUserIdEncrypted { get; set; }
    public string? AadeSubscriptionKeyEncrypted { get; set; }

    /// <summary>
    /// Last successful AADE sync timestamp (UTC). Null = never synced. Set by
    /// <see cref="RecordAadeSync"/> at the end of a successful sync run.
    /// </summary>
    public DateTime? LastAadeSyncAt { get; private set; }

    /// <summary>Last-seen AADE mark for incoming (RequestDocs) invoices. Null = never synced.</summary>
    public long? LastAadeIncomingMark { get; private set; }

    /// <summary>Last-seen AADE mark for outgoing (RequestMyIncome) invoices. Null = never synced.</summary>
    public long? LastAadeOutgoingMark { get; private set; }

    /// <summary>
    /// True while a persistent AADE auth failure (e.g., credentials revoked) is
    /// unresolved. Cleared on successful sync, reconnect, or disconnect. Transient
    /// network/server errors do NOT set this — only failures that need user action.
    /// </summary>
    public bool HasAadeFailure { get; private set; }

    /// <summary>UTC instant the current failure period STARTED — preserved across
    /// repeat failures so the 24-hour notification window measures elapsed broken
    /// time, not the most recent attempt.</summary>
    public DateTime? AadeLastFailureAt { get; private set; }

    /// <summary>Short failure-category tag (e.g., "InvalidCredentials") used to
    /// pick a user-facing message. Not localised — UI translates.</summary>
    public string? AadeLastFailureReason { get; private set; }

    /// <summary>UTC instant the 24-hour failure notification email was sent for the
    /// current failure period. Prevents duplicate notifications until the failure
    /// is cleared.</summary>
    public DateTime? AadeFailureEmailSentAt { get; private set; }

    // Required for EF Core materialization. Not callable externally — use Business.Create(...) instead.
    private Business() { }

    /// <summary>
    /// Creates a new <see cref="Business"/> with validated initial state.
    /// </summary>
    /// <exception cref="InvalidAfmException">AFM fails the Greek tax-ID checksum.</exception>
    /// <exception cref="DomainException">Name is empty.</exception>
    public static Business Create(string name, string afm, string? kad, string? address)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(afm);

        var trimmedName = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
            throw new DomainException("Business name is required.");

        var trimmedAfm = afm.Trim();
        if (!AfmValidator.IsValid(trimmedAfm))
            throw new InvalidAfmException(trimmedAfm);

        return new Business
        {
            Name = trimmedName,
            Afm = trimmedAfm,
            Kad = NormalizeOptional(kad),
            Address = NormalizeOptional(address)
        };
    }

    public void Rename(string newName)
    {
        ArgumentNullException.ThrowIfNull(newName);
        var trimmed = newName.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new DomainException("Business name cannot be empty.");
        Name = trimmed;
    }

    public void ChangeAfm(string newAfm)
    {
        ArgumentNullException.ThrowIfNull(newAfm);
        var trimmed = newAfm.Trim();
        if (!AfmValidator.IsValid(trimmed))
            throw new InvalidAfmException(trimmed);
        Afm = trimmed;
    }

    public void UpdateKad(string? newKad) => Kad = NormalizeOptional(newKad);

    public void UpdateAddress(string? newAddress) => Address = NormalizeOptional(newAddress);

    public void Deactivate()
    {
        if (!IsActive)
            throw new DomainException("Business is already inactive.");
        IsActive = false;
    }

    public void Reactivate()
    {
        if (IsActive)
            throw new DomainException("Business is already active.");
        IsActive = true;
    }

    /// <summary>Records that an AADE sync completed at the given UTC instant.</summary>
    public void RecordAadeSync(DateTime syncedAtUtc) => LastAadeSyncAt = syncedAtUtc;

    /// <summary>
    /// Updates the recorded last-seen marks after a successful sync. Both are
    /// updated together to maintain a consistent view of where we are in
    /// AADE's invoice sequence. Pass null to leave a side unchanged. Marks only
    /// ever advance forward — a lower incoming value is ignored, preventing
    /// regression if AADE returns an unexpected older mark.
    /// </summary>
    public void RecordAadeSyncProgress(long? newIncomingMark, long? newOutgoingMark)
    {
        if (newIncomingMark.HasValue && (LastAadeIncomingMark is null || newIncomingMark > LastAadeIncomingMark))
            LastAadeIncomingMark = newIncomingMark;

        if (newOutgoingMark.HasValue && (LastAadeOutgoingMark is null || newOutgoingMark > LastAadeOutgoingMark))
            LastAadeOutgoingMark = newOutgoingMark;

        LastAadeSyncAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records that an AADE sync attempt failed in a way that needs user
    /// intervention. <paramref name="reason"/> is a short category tag the UI
    /// translates ("InvalidCredentials", "AfmMismatch", etc.). The failure
    /// start timestamp is fixed on the first failure of a streak so the
    /// 24-hour notification window measures elapsed broken time.
    /// </summary>
    public void RecordAadeSyncFailure(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (!HasAadeFailure)
            AadeLastFailureAt = DateTime.UtcNow;

        HasAadeFailure = true;
        AadeLastFailureReason = reason;
    }

    /// <summary>
    /// Clears the AADE failure state. Called on successful sync OR when the
    /// user disconnects/reconnects credentials. Resets the email-sent stamp so
    /// a future failure period can re-trigger the 24-hour notification.
    /// </summary>
    public void ClearAadeSyncFailure()
    {
        HasAadeFailure = false;
        AadeLastFailureAt = null;
        AadeLastFailureReason = null;
        AadeFailureEmailSentAt = null;
    }

    /// <summary>
    /// Marks that the 24-hour failure notification email has been sent for the
    /// current failure period. Guards against double-send by requiring an
    /// active failure.
    /// </summary>
    public void RecordAadeFailureEmailSent()
    {
        if (!HasAadeFailure)
            throw new InvalidOperationException("Cannot record email sent — no active AADE failure.");

        AadeFailureEmailSentAt = DateTime.UtcNow;
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
