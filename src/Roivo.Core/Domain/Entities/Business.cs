using Roivo.Core.Domain.Enums;
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

    /// <summary>
    /// How often this business files ΦΠΑ. Drives which obligations the tax
    /// calendar generates. Stored rather than derived from turnover: the switch
    /// takes effect from a tax year the accountant decides.
    /// </summary>
    public VatFrequency VatFrequency { get; private set; } = VatFrequency.Quarterly;

    /// <summary>
    /// Taxable value of property the business owns, for the ΕΝΦΙΑ estimate.
    /// Null when unknown, in which case no ΕΝΦΙΑ obligation is projected —
    /// Roivo cannot see the property register, so a guess would be fabrication.
    /// </summary>
    public decimal? EstimatedPropertyValue { get; private set; }

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

    // The banking session is written exclusively through
    // IBankingCredentialStore, which encrypts before storing. This column holds
    // cipher-text — never plaintext. UI/handlers go through the store, never
    // read it directly.
    public string? BankingAccessTokenEncrypted { get; set; }

    /// <summary>Name of the connected bank, as the aggregator spells it. Not a
    /// secret — shown in the UI and used to restart a lapsed consent.</summary>
    public string? BankingProviderName { get; private set; }

    /// <summary>UTC instant the bank-side consent lapses. PSD2 caps unattended
    /// access at 90 days, after which the user must re-consent.</summary>
    public DateTime? BankingConsentExpiresAt { get; private set; }

    /// <summary>Last successful banking sync (UTC). Null = never synced.</summary>
    public DateTime? LastBankingSyncAt { get; private set; }

    /// <summary>Consecutive failed banking syncs. Reset to zero by any success,
    /// so a non-zero value always means "currently broken", not "broke once".</summary>
    public int BankingSyncErrorCount { get; private set; }

    /// <summary>UTC instant the current failure streak started — preserved
    /// across repeat failures so the notification window measures elapsed broken
    /// time, not the most recent attempt.</summary>
    public DateTime? BankingFirstFailureAt { get; private set; }

    /// <summary>Short failure-category tag ("SessionExpired", "NetworkError")
    /// used to pick a user-facing message. Not localised — the UI translates.</summary>
    public string? BankingLastFailureReason { get; private set; }

    /// <summary>UTC instant the failure notification email was sent for the
    /// current failure streak. Prevents duplicates until the failure clears.</summary>
    public DateTime? BankingFailureEmailSentAt { get; private set; }

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

    /// <summary>
    /// Records that a bank connection was established (or re-established).
    /// Clears any prior failure state — a fresh consent heals the banner.
    /// </summary>
    public void RecordBankingConnection(string providerName, DateTime? consentExpiresAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);

        BankingProviderName = providerName.Trim();
        BankingConsentExpiresAt = consentExpiresAtUtc;
        ClearBankingSyncFailure();
    }

    /// <summary>Records a successful banking sync at the given UTC instant and
    /// clears any failure streak.</summary>
    public void RecordBankingSyncSuccess(DateTime syncedAtUtc)
    {
        LastBankingSyncAt = syncedAtUtc;
        ClearBankingSyncFailure();
    }

    /// <summary>
    /// Records a failed banking sync. <paramref name="reason"/> is a short
    /// category tag the UI translates. The streak start is fixed on the first
    /// failure so the notification window measures elapsed broken time.
    /// </summary>
    public void RecordBankingSyncFailure(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (BankingSyncErrorCount == 0)
            BankingFirstFailureAt = DateTime.UtcNow;

        BankingSyncErrorCount++;
        BankingLastFailureReason = reason;
    }

    /// <summary>
    /// Clears banking failure state. Called on successful sync, reconnect or
    /// disconnect. Resets the email stamp so a future streak can notify again.
    /// </summary>
    public void ClearBankingSyncFailure()
    {
        BankingSyncErrorCount = 0;
        BankingFirstFailureAt = null;
        BankingLastFailureReason = null;
        BankingFailureEmailSentAt = null;
    }

    /// <summary>
    /// Forgets the bank connection entirely. The encrypted session itself is
    /// cleared by IBankingCredentialStore; this drops the metadata around it.
    /// </summary>
    public void ClearBankingConnection()
    {
        BankingProviderName = null;
        BankingConsentExpiresAt = null;
        LastBankingSyncAt = null;
        ClearBankingSyncFailure();
    }

    /// <summary>
    /// Marks that the failure notification email has been sent for the current
    /// streak. Guards against double-send by requiring an active failure.
    /// </summary>
    public void RecordBankingFailureEmailSent()
    {
        if (BankingSyncErrorCount == 0)
            throw new InvalidOperationException("Cannot record email sent — no active banking failure.");

        BankingFailureEmailSentAt = DateTime.UtcNow;
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Changes the ΦΠΑ filing frequency.</summary>
    public void SetVatFrequency(VatFrequency frequency) => VatFrequency = frequency;

    /// <summary>
    /// Records the taxable property value used for the ΕΝΦΙΑ estimate. Pass
    /// null to clear it, which stops ΕΝΦΙΑ being projected at all.
    /// </summary>
    /// <exception cref="DomainException">The value is negative.</exception>
    public void SetEstimatedPropertyValue(decimal? value)
    {
        if (value is < 0m)
            throw new DomainException("Estimated property value cannot be negative.");

        EstimatedPropertyValue = value;
    }
}
