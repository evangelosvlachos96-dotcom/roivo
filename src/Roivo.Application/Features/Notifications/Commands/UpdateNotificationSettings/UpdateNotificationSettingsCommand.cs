namespace Roivo.Application.Features.Notifications.Commands.UpdateNotificationSettings;

/// <param name="UserId">Identity user id, as it appears on the principal's name-identifier claim.</param>
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
