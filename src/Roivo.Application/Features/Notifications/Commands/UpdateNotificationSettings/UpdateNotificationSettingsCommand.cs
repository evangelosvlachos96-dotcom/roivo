namespace Roivo.Application.Features.Notifications.Commands.UpdateNotificationSettings;

/// <summary>The full set of notification preferences for one user on one business.</summary>
/// <param name="BusinessId">The business these preferences apply to.</param>
/// <param name="UserId">Identity user id, as it appears on the principal's name-identifier claim.</param>
/// <param name="DailyDigestEnabled">Whether to send the daily cashflow digest.</param>
/// <param name="TaxReminderEnabled">Whether to send reminders ahead of tax due dates.</param>
/// <param name="TaxReminderDaysBefore">How many days ahead of a due date the reminder goes out.</param>
/// <param name="CashflowAlertEnabled">Whether to alert when the projected balance runs low.</param>
/// <param name="CashflowAlertThreshold">Projected balance at or below which the cashflow alert fires.</param>
/// <param name="SyncFailureAlertEnabled">Whether to alert when an AADE or bank sync keeps failing.</param>
/// <param name="WeeklyReconciliationEnabled">Whether to send the weekly reconciliation summary.</param>
public sealed record UpdateNotificationSettingsCommand(
    Guid BusinessId,
    string UserId,
    bool DailyDigestEnabled,
    bool TaxReminderEnabled,
    int TaxReminderDaysBefore,
    bool CashflowAlertEnabled,
    decimal CashflowAlertThreshold,
    bool SyncFailureAlertEnabled,
    bool WeeklyReconciliationEnabled);
