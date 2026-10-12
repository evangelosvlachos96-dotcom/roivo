using Roivo.Core.Domain.Exceptions;
using Roivo.Core.Domain.Interfaces;

namespace Roivo.Core.Domain.Entities;

/// <summary>
/// One user's notification preferences for one business.
/// </summary>
/// <remarks>
/// Per user and per business rather than per business alone: an accountant
/// wants a digest across their clients while the owner of one of those
/// businesses wants only their own alerts, and the two must not overwrite each
/// other's choices.
/// </remarks>
public class NotificationSettings : ITenantScoped
{
    /// <summary>Default lead time for a tax reminder, in days.</summary>
    public const int DefaultTaxReminderDaysBefore = 7;

    /// <summary>Longest lead time a reminder may be set to.</summary>
    public const int MaxTaxReminderDaysBefore = 60;

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }

    public Guid BusinessId { get; private set; }
    public Business? Business { get; private set; }

    /// <summary>Identity user these preferences belong to.</summary>
    public string UserId { get; private set; } = null!;

    public bool DailyDigestEnabled { get; private set; }

    public bool TaxReminderEnabled { get; private set; } = true;

    /// <summary>How many days before a due date the first reminder goes out.</summary>
    public int TaxReminderDaysBefore { get; private set; } = DefaultTaxReminderDaysBefore;

    public bool CashflowAlertEnabled { get; private set; } = true;

    /// <summary>
    /// Projected balance at or below which a cashflow alert fires. Zero means
    /// "alert once the balance is predicted to go negative".
    /// </summary>
    public decimal CashflowAlertThreshold { get; private set; }

    public bool SyncFailureAlertEnabled { get; private set; } = true;

    public bool WeeklyReconciliationEnabled { get; private set; }

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    // Required for EF Core materialization. Use NotificationSettings.CreateDefault(...).
    private NotificationSettings() { }

    /// <summary>
    /// Creates the default preferences for a user. The digest and the weekly
    /// reconciliation summary default on only for accountants, who are the ones
    /// watching a portfolio; a single business owner opening their own books
    /// does not need a daily summary of one business.
    /// </summary>
    public static NotificationSettings CreateDefault(Guid businessId, string userId, bool isAccountant)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return new NotificationSettings
        {
            BusinessId = businessId,
            UserId = userId,
            DailyDigestEnabled = isAccountant,
            WeeklyReconciliationEnabled = isAccountant,
        };
    }

    /// <exception cref="DomainException">The reminder lead time is out of range.</exception>
    public void Update(
        bool dailyDigestEnabled,
        bool taxReminderEnabled,
        int taxReminderDaysBefore,
        bool cashflowAlertEnabled,
        decimal cashflowAlertThreshold,
        bool syncFailureAlertEnabled,
        bool weeklyReconciliationEnabled)
    {
        if (taxReminderDaysBefore is < 1 or > MaxTaxReminderDaysBefore)
        {
            throw new DomainException(
                $"Tax reminder lead time must be between 1 and {MaxTaxReminderDaysBefore} days; got {taxReminderDaysBefore}.");
        }

        DailyDigestEnabled = dailyDigestEnabled;
        TaxReminderEnabled = taxReminderEnabled;
        TaxReminderDaysBefore = taxReminderDaysBefore;
        CashflowAlertEnabled = cashflowAlertEnabled;
        CashflowAlertThreshold = cashflowAlertThreshold;
        SyncFailureAlertEnabled = syncFailureAlertEnabled;
        WeeklyReconciliationEnabled = weeklyReconciliationEnabled;
        UpdatedAt = DateTime.UtcNow;
    }
}
