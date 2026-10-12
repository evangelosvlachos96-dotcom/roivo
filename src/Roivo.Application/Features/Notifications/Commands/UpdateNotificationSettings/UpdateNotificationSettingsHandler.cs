using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Auditing;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Enums;
using Roivo.Core.Domain.Exceptions;

namespace Roivo.Application.Features.Notifications.Commands.UpdateNotificationSettings;

/// <summary>
/// Saves a user's notification preferences for one business, creating the row
/// on first save.
/// </summary>
public sealed class UpdateNotificationSettingsHandler
{
    private readonly INotificationSettingsRepository _repository;
    private readonly IBusinessRepository _businesses;
    private readonly IAuditWriter _audit;
    private readonly ITenantContext _tenant;

    public UpdateNotificationSettingsHandler(
        INotificationSettingsRepository repository,
        IBusinessRepository businesses,
        IAuditWriter audit,
        ITenantContext tenant)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(businesses);
        ArgumentNullException.ThrowIfNull(audit);
        ArgumentNullException.ThrowIfNull(tenant);

        _repository = repository;
        _businesses = businesses;
        _audit = audit;
        _tenant = tenant;
    }

    public async Task<UpdateNotificationSettingsResult> Handle(
        UpdateNotificationSettingsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Notification preferences are the user's own, so there is no role gate
        // here the way there is on creating a business: both tenant types manage
        // their own. What must hold is that a tenant is actually in scope — with
        // Guid.Empty the tenant-filtered reads below match nothing and a write
        // would land on a row no one can see again.
        if (_tenant.CurrentTenantId == Guid.Empty || string.IsNullOrWhiteSpace(command.UserId))
            return new UpdateNotificationSettingsResult.Forbidden();

        var business = await _businesses
            .GetByIdActiveOnlyAsync(command.BusinessId, cancellationToken)
            .ConfigureAwait(false);

        if (business is null)
            return new UpdateNotificationSettingsResult.NotFound();

        var settings = await _repository
            .GetAsync(command.BusinessId, command.UserId, cancellationToken)
            .ConfigureAwait(false)
            ?? NotificationSettings.CreateDefault(
                command.BusinessId,
                command.UserId,
                isAccountant: _tenant.CurrentTenantType == TenantType.Accountant);

        try
        {
            settings.Update(
                command.DailyDigestEnabled,
                command.TaxReminderEnabled,
                command.TaxReminderDaysBefore,
                command.CashflowAlertEnabled,
                command.CashflowAlertThreshold,
                command.SyncFailureAlertEnabled,
                command.WeeklyReconciliationEnabled);
        }
        catch (DomainException ex)
        {
            return new UpdateNotificationSettingsResult.Invalid(ex.Message);
        }

        var saved = await _repository.UpsertAsync(settings, cancellationToken).ConfigureAwait(false);

        await _audit.WriteAsync(
            action: AuditAction.NotificationSettingsUpdated,
            tenantId: _tenant.CurrentTenantId,
            entityType: nameof(NotificationSettings),
            entityId: saved.Id.ToString(),
            details: new Dictionary<string, object?>
            {
                ["BusinessId"] = saved.BusinessId,
                ["UserId"] = saved.UserId,
                ["DailyDigest"] = saved.DailyDigestEnabled,
                ["TaxReminder"] = saved.TaxReminderEnabled,
                ["TaxReminderDaysBefore"] = saved.TaxReminderDaysBefore,
                ["CashflowAlert"] = saved.CashflowAlertEnabled,
                ["CashflowAlertThreshold"] = saved.CashflowAlertThreshold,
                ["SyncFailureAlert"] = saved.SyncFailureAlertEnabled,
                ["WeeklyReconciliation"] = saved.WeeklyReconciliationEnabled,
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new UpdateNotificationSettingsResult.Success(
            NotificationSettingsView.From(saved, isPersisted: true));
    }
}
