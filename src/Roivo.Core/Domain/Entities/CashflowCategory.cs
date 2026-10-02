using Roivo.Core.Domain.Enums;
using Roivo.Core.Domain.Exceptions;
using Roivo.Core.Domain.Interfaces;

namespace Roivo.Core.Domain.Entities;

/// <summary>
/// A named recurring or one-off cashflow line a business tells Roivo about —
/// rent, payroll, a subscription.
/// </summary>
/// <remarks>
/// History alone cannot predict a commitment the business has made but not yet
/// paid from this account, so recurring items are layered onto the statistical
/// forecast rather than inferred from it.
/// </remarks>
public class CashflowCategory : ITenantScoped
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }

    public Guid BusinessId { get; private set; }
    public Business? Business { get; private set; }

    public string Name { get; private set; } = null!;

    public CashflowCategoryType Type { get; private set; }

    public bool IsRecurring { get; private set; }

    /// <summary>
    /// Day of month the item falls due, 1–31. Days past the end of a short month
    /// are clamped to that month's last day when the forecast is projected.
    /// </summary>
    public int? RecurringDay { get; private set; }

    /// <summary>Unsigned magnitude. <see cref="Type"/> carries the direction.</summary>
    public decimal? AverageAmount { get; private set; }

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Required for EF Core materialization. Use CashflowCategory.Create(...).
    private CashflowCategory() { }

    /// <exception cref="DomainException">Name empty, day out of range, or amount negative.</exception>
    public static CashflowCategory Create(
        Guid businessId,
        string name,
        CashflowCategoryType type,
        bool isRecurring,
        int? recurringDay,
        decimal? averageAmount)
    {
        ArgumentNullException.ThrowIfNull(name);

        var trimmed = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new DomainException("Cashflow category name is required.");

        GuardSchedule(isRecurring, recurringDay);
        GuardAmount(averageAmount);

        return new CashflowCategory
        {
            BusinessId = businessId,
            Name = trimmed,
            Type = type,
            IsRecurring = isRecurring,
            RecurringDay = isRecurring ? recurringDay : null,
            AverageAmount = averageAmount,
        };
    }

    public void Rename(string newName)
    {
        ArgumentNullException.ThrowIfNull(newName);
        var trimmed = newName.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new DomainException("Cashflow category name cannot be empty.");
        Name = trimmed;
    }

    public void UpdateSchedule(bool isRecurring, int? recurringDay)
    {
        GuardSchedule(isRecurring, recurringDay);
        IsRecurring = isRecurring;
        RecurringDay = isRecurring ? recurringDay : null;
    }

    public void UpdateAverageAmount(decimal? averageAmount)
    {
        GuardAmount(averageAmount);
        AverageAmount = averageAmount;
    }

    /// <summary>
    /// The day this item falls on in the given month, clamped to the month's
    /// length so a 31st-of-the-month item still lands in February.
    /// </summary>
    public DateOnly? OccurrenceIn(int year, int month)
    {
        if (!IsRecurring || RecurringDay is not { } day)
            return null;

        var lastDay = DateTime.DaysInMonth(year, month);
        return new DateOnly(year, month, Math.Min(day, lastDay));
    }

    /// <summary>Signed contribution to a daily net: negative for expenses.</summary>
    public decimal SignedAmount => Type == CashflowCategoryType.Expense
        ? -(AverageAmount ?? 0m)
        : AverageAmount ?? 0m;

    private static void GuardSchedule(bool isRecurring, int? recurringDay)
    {
        if (!isRecurring)
            return;

        if (recurringDay is not { } day)
            throw new DomainException("A recurring category needs a day of month.");

        if (day is < 1 or > 31)
            throw new DomainException($"Recurring day must be between 1 and 31; got {day}.");
    }

    private static void GuardAmount(decimal? averageAmount)
    {
        if (averageAmount is < 0m)
            throw new DomainException("Average amount cannot be negative; direction comes from the category type.");
    }
}
