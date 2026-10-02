using Roivo.Core.Domain.Enums;
using Roivo.Core.Domain.Exceptions;
using Roivo.Core.Domain.Interfaces;

namespace Roivo.Core.Domain.Entities;

/// <summary>
/// A Greek tax or contribution payment falling due on a known date, with an
/// estimate until the real figure is known.
/// </summary>
/// <remarks>
/// Tax is the largest predictable outflow a Greek SMB faces and the one most
/// likely to break a cash position, so obligations are first-class rows the
/// forecast reads rather than a derived calculation.
/// </remarks>
public class TaxObligation : ITenantScoped
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }

    public Guid BusinessId { get; private set; }
    public Business? Business { get; private set; }

    public TaxType TaxType { get; private set; }

    public DateOnly DueDate { get; private set; }

    public decimal EstimatedAmount { get; private set; }

    public decimal? ActualAmount { get; private set; }

    public bool IsPaid { get; private set; }

    public DateTime? PaidAt { get; private set; }

    /// <summary>
    /// Period identifier the obligation settles, e.g. <c>2026-Q3</c> for VAT or
    /// <c>2026-07</c> for a monthly item. Together with
    /// <see cref="TaxType"/> and <see cref="BusinessId"/> it uniquely identifies
    /// the obligation, which is what lets the calendar regenerate without
    /// creating duplicates.
    /// </summary>
    public string Period { get; private set; } = null!;

    public string? Notes { get; private set; }

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Required for EF Core materialization. Use TaxObligation.Create(...).
    private TaxObligation() { }

    /// <exception cref="DomainException">Period empty or estimate negative.</exception>
    public static TaxObligation Create(
        Guid businessId,
        TaxType taxType,
        DateOnly dueDate,
        decimal estimatedAmount,
        string period,
        string? notes = null)
    {
        ArgumentNullException.ThrowIfNull(period);

        var trimmedPeriod = period.Trim();
        if (string.IsNullOrWhiteSpace(trimmedPeriod))
            throw new DomainException("Tax obligation period is required.");

        if (estimatedAmount < 0m)
            throw new DomainException("Estimated tax amount cannot be negative.");

        return new TaxObligation
        {
            BusinessId = businessId,
            TaxType = taxType,
            DueDate = dueDate,
            EstimatedAmount = estimatedAmount,
            Period = trimmedPeriod,
            Notes = NormalizeOptional(notes),
        };
    }

    /// <summary>Revises the estimate. Refused once paid — the actual figure stands.</summary>
    /// <exception cref="DomainException">Already paid, or the estimate is negative.</exception>
    public void ReviseEstimate(decimal estimatedAmount)
    {
        if (IsPaid)
            throw new DomainException("Cannot revise the estimate of a paid obligation.");
        if (estimatedAmount < 0m)
            throw new DomainException("Estimated tax amount cannot be negative.");

        EstimatedAmount = estimatedAmount;
    }

    /// <exception cref="DomainException">Already paid, or the amount is negative.</exception>
    public void MarkPaid(decimal actualAmount, DateTime paidAtUtc)
    {
        if (IsPaid)
            throw new DomainException("Tax obligation is already marked paid.");
        if (actualAmount < 0m)
            throw new DomainException("Paid amount cannot be negative.");

        IsPaid = true;
        ActualAmount = actualAmount;
        PaidAt = paidAtUtc;
    }

    /// <exception cref="DomainException">The obligation is not marked paid.</exception>
    public void ReopenAsUnpaid()
    {
        if (!IsPaid)
            throw new DomainException("Tax obligation is not marked paid.");

        IsPaid = false;
        ActualAmount = null;
        PaidAt = null;
    }

    public void UpdateNotes(string? notes) => Notes = NormalizeOptional(notes);

    /// <summary>The figure the forecast should subtract: the actual once known, else the estimate.</summary>
    public decimal ExpectedAmount => ActualAmount ?? EstimatedAmount;

    /// <summary>Unpaid and past its due date as of <paramref name="today"/>.</summary>
    public bool IsOverdue(DateOnly today) => !IsPaid && DueDate < today;

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
