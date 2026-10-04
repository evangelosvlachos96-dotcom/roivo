using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Features.Notifications;

/// <summary>
/// The notification preferences as the settings page needs them.
/// </summary>
/// <remarks>
/// A DTO rather than the entity because the page round-trips these values
/// through two-way bindings; handing a Razor component an entity with private
/// setters would force it to build a parallel input model anyway, and handing it
/// a mutable entity would invite it to mutate domain state outside a handler.
/// <see cref="IsPersisted"/> tells the page whether it is looking at stored
/// choices or at defaults nobody has saved yet.
/// </remarks>
public sealed record NotificationSettingsView(
    Guid BusinessId,
    string UserId,
    bool DailyDigestEnabled,
    bool TaxReminderEnabled,
    int TaxReminderDaysBefore,
    bool CashflowAlertEnabled,
    decimal CashflowAlertThreshold,
    bool SyncFailureAlertEnabled,
    bool WeeklyReconciliationEnabled,
    bool IsPersisted)
{
    /// <summary>Longest lead time the domain accepts, surfaced for input validation.</summary>
    public static int MaxTaxReminderDaysBefore => NotificationSettings.MaxTaxReminderDaysBefore;

    internal static NotificationSettingsView From(NotificationSettings settings, bool isPersisted) => new(
        settings.BusinessId,
        settings.UserId,
        settings.DailyDigestEnabled,
        settings.TaxReminderEnabled,
        settings.TaxReminderDaysBefore,
        settings.CashflowAlertEnabled,
        settings.CashflowAlertThreshold,
        settings.SyncFailureAlertEnabled,
        settings.WeeklyReconciliationEnabled,
        isPersisted);
}
