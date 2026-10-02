using Roivo.Core.Domain.Exceptions;
using Roivo.Core.Domain.Interfaces;

namespace Roivo.Core.Domain.Entities;

/// <summary>
/// Per-business tuning for the matching engine. A business with no rule is
/// matched on <see cref="Defaults"/>.
/// </summary>
public class ReconciliationRule : ITenantScoped
{
    /// <summary>Tolerances applied when a business has defined no rule of its own.</summary>
    public static class Defaults
    {
        public const decimal AmountTolerancePercent = 0.5m;
        public const int DateToleranceDays = 3;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }

    public Guid BusinessId { get; private set; }
    public Business? Business { get; private set; }

    public string RuleName { get; private set; } = null!;

    /// <summary>Percentage points, not a fraction: 0.5 means ±0.5%.</summary>
    public decimal AmountTolerancePercent { get; private set; } = Defaults.AmountTolerancePercent;

    public int DateToleranceDays { get; private set; } = Defaults.DateToleranceDays;

    /// <summary>
    /// Optional substring matched case-insensitively against the transaction
    /// counterparty and reference. Deliberately not a regex: these are authored
    /// by accountants, and an untrusted pattern is a denial-of-service risk
    /// against the nightly job.
    /// </summary>
    public string? CounterpartyPattern { get; private set; }

    public bool IsActive { get; private set; } = true;

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Required for EF Core materialization. Use ReconciliationRule.Create(...).
    private ReconciliationRule() { }

    /// <exception cref="DomainException">Name is empty or a tolerance is negative.</exception>
    public static ReconciliationRule Create(
        Guid businessId,
        string ruleName,
        decimal amountTolerancePercent,
        int dateToleranceDays,
        string? counterpartyPattern)
    {
        ArgumentNullException.ThrowIfNull(ruleName);

        var trimmed = ruleName.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new DomainException("Reconciliation rule name is required.");

        GuardTolerances(amountTolerancePercent, dateToleranceDays);

        return new ReconciliationRule
        {
            BusinessId = businessId,
            RuleName = trimmed,
            AmountTolerancePercent = amountTolerancePercent,
            DateToleranceDays = dateToleranceDays,
            CounterpartyPattern = NormalizeOptional(counterpartyPattern),
        };
    }

    public void UpdateTolerances(decimal amountTolerancePercent, int dateToleranceDays)
    {
        GuardTolerances(amountTolerancePercent, dateToleranceDays);
        AmountTolerancePercent = amountTolerancePercent;
        DateToleranceDays = dateToleranceDays;
    }

    public void UpdateCounterpartyPattern(string? pattern)
        => CounterpartyPattern = NormalizeOptional(pattern);

    public void Deactivate()
    {
        if (!IsActive)
            throw new DomainException("Reconciliation rule is already inactive.");
        IsActive = false;
    }

    public void Reactivate()
    {
        if (IsActive)
            throw new DomainException("Reconciliation rule is already active.");
        IsActive = true;
    }

    private static void GuardTolerances(decimal amountTolerancePercent, int dateToleranceDays)
    {
        if (amountTolerancePercent < 0m)
            throw new DomainException("Amount tolerance cannot be negative.");
        if (dateToleranceDays < 0)
            throw new DomainException("Date tolerance cannot be negative.");
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
