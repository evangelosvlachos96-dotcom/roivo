using Roivo.Core.Domain.Enums;
using Roivo.Core.Domain.Exceptions;
using Roivo.Core.Domain.Interfaces;

namespace Roivo.Core.Domain.Entities;

/// <summary>
/// A link between an <see cref="Invoice"/> and a <see cref="BankTransaction"/>
/// asserting they are the same money.
/// </summary>
/// <remarks>
/// A rejected match is kept rather than deleted: it is the only record that the
/// engine already proposed this pair and a human said no, which is what stops
/// the next run from proposing it again.
/// </remarks>
public class ReconciliationMatch : ITenantScoped
{
    /// <summary>Score at or above which a match is applied without review.</summary>
    public const decimal AutomaticThreshold = 0.8m;

    /// <summary>Score at or above which a pair is worth showing to a user.</summary>
    public const decimal SuggestedThreshold = 0.5m;

    public Guid Id { get; private set; } = Guid.NewGuid();

    // Set by ApplicationDbContext.SaveChangesAsync; public setter is the
    // documented exception to the private-setter rule.
    public Guid TenantId { get; set; }

    public Guid BusinessId { get; private set; }
    public Business? Business { get; private set; }

    public Guid InvoiceId { get; private set; }
    public Invoice? Invoice { get; private set; }

    public Guid BankTransactionId { get; private set; }
    public BankTransaction? BankTransaction { get; private set; }

    public ReconciliationMatchType MatchType { get; private set; }

    /// <summary>Engine score in [0,1]. Manual matches are recorded as 1.0.</summary>
    public decimal MatchConfidence { get; private set; }

    public ReconciliationMatchStatus Status { get; private set; }

    public DateTime MatchedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>Identity user id of the person who created or decided the match. Null for engine-created ones.</summary>
    public string? MatchedByUserId { get; private set; }

    public string? Notes { get; private set; }

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    // Required for EF Core materialization. Use the static factories instead.
    private ReconciliationMatch() { }

    /// <summary>
    /// Creates an engine-produced match. A score at or above
    /// <see cref="AutomaticThreshold"/> is confirmed immediately; anything lower
    /// is left <see cref="ReconciliationMatchStatus.Pending"/> for review.
    /// </summary>
    /// <exception cref="DomainException">Confidence is outside [0,1].</exception>
    public static ReconciliationMatch CreateFromEngine(
        Guid businessId,
        Guid invoiceId,
        Guid bankTransactionId,
        decimal confidence)
    {
        GuardConfidence(confidence);

        var automatic = confidence >= AutomaticThreshold;

        return new ReconciliationMatch
        {
            BusinessId = businessId,
            InvoiceId = invoiceId,
            BankTransactionId = bankTransactionId,
            MatchConfidence = confidence,
            MatchType = automatic ? ReconciliationMatchType.Automatic : ReconciliationMatchType.Suggested,
            Status = automatic ? ReconciliationMatchStatus.Confirmed : ReconciliationMatchStatus.Pending,
        };
    }

    /// <summary>
    /// Creates a user-asserted match. A person pairing two rows by hand is a
    /// stronger signal than any score, so it is confirmed at full confidence.
    /// </summary>
    public static ReconciliationMatch CreateManual(
        Guid businessId,
        Guid invoiceId,
        Guid bankTransactionId,
        string userId,
        string? notes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return new ReconciliationMatch
        {
            BusinessId = businessId,
            InvoiceId = invoiceId,
            BankTransactionId = bankTransactionId,
            MatchConfidence = 1.0m,
            MatchType = ReconciliationMatchType.Manual,
            Status = ReconciliationMatchStatus.Confirmed,
            MatchedByUserId = userId,
            Notes = NormalizeOptional(notes),
        };
    }

    /// <summary>Accepts a pending suggestion.</summary>
    /// <exception cref="DomainException">The match is not pending.</exception>
    public void Confirm(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        if (Status != ReconciliationMatchStatus.Pending)
            throw new DomainException($"Only a pending match can be confirmed; this one is {Status}.");

        Status = ReconciliationMatchStatus.Confirmed;
        MatchedByUserId = userId;
        MatchedAt = DateTime.UtcNow;
        Touch();
    }

    /// <summary>Declines a pending suggestion.</summary>
    /// <exception cref="DomainException">The match is not pending.</exception>
    public void Reject(string userId, string? notes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        if (Status != ReconciliationMatchStatus.Pending)
            throw new DomainException($"Only a pending match can be rejected; this one is {Status}.");

        Status = ReconciliationMatchStatus.Rejected;
        MatchedByUserId = userId;
        Notes = NormalizeOptional(notes) ?? Notes;
        Touch();
    }

    public void UpdateNotes(string? notes)
    {
        Notes = NormalizeOptional(notes);
        Touch();
    }

    /// <summary>True when this match should mark both sides reconciled.</summary>
    public bool IsEffective => Status == ReconciliationMatchStatus.Confirmed;

    private void Touch() => UpdatedAt = DateTime.UtcNow;

    private static void GuardConfidence(decimal confidence)
    {
        if (confidence is < 0m or > 1m)
            throw new DomainException($"Match confidence must be between 0 and 1; got {confidence}.");
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
