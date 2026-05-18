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

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
