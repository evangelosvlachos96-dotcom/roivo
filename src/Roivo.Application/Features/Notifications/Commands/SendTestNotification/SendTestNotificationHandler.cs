using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Auditing;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Features.Notifications.Commands.SendTestNotification;

/// <summary>
/// Sends the signed-in user one sample notification email so they can see what
/// a given alert looks like before waiting for the real thing.
/// </summary>
public sealed class SendTestNotificationHandler
{
    private readonly INotificationSettingsRepository _repository;
    private readonly IBusinessRepository _businesses;
    private readonly INotificationEmailDispatcher _dispatcher;
    private readonly IAuditWriter _audit;
    private readonly ITenantContext _tenant;

    public SendTestNotificationHandler(
        INotificationSettingsRepository repository,
        IBusinessRepository businesses,
        INotificationEmailDispatcher dispatcher,
        IAuditWriter audit,
        ITenantContext tenant)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(businesses);
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(audit);
        ArgumentNullException.ThrowIfNull(tenant);

        _repository = repository;
        _businesses = businesses;
        _dispatcher = dispatcher;
        _audit = audit;
        _tenant = tenant;
    }

    public async Task<SendTestNotificationResult> Handle(
        SendTestNotificationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (_tenant.CurrentTenantId == Guid.Empty || string.IsNullOrWhiteSpace(command.UserId))
            return new SendTestNotificationResult.Forbidden();

        var business = await _businesses
            .GetByIdActiveOnlyAsync(command.BusinessId, cancellationToken)
            .ConfigureAwait(false);

        if (business is null)
            return new SendTestNotificationResult.NotFound();

        var email = await _repository
            .GetConfirmedEmailAsync(command.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(email))
            return new SendTestNotificationResult.SendFailed(SendTestNotificationResult.NoConfirmedEmailReason);

        try
        {
            await _dispatcher
                .SendSampleAsync(command.Kind, email, business.Id, business.Name, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A send failure is an expected outcome of pressing this button —
            // a bad API key, a provider outage, a rejected address. The user
            // needs to see which, so it becomes a result rather than a 500.
            // OperationCanceledException is excluded: that is the request going
            // away, not the transport failing.
            return new SendTestNotificationResult.SendFailed(ex.Message);
        }

        await _audit.WriteAsync(
            action: AuditAction.TestNotificationSent,
            tenantId: _tenant.CurrentTenantId,
            entityType: nameof(NotificationSettings),
            entityId: business.Id.ToString(),
            details: new Dictionary<string, object?>
            {
                ["Kind"] = command.Kind.ToString(),
                ["UserId"] = command.UserId,
                // The address is the user's own and already on their account
                // record, so recording it here leaks nothing new and makes
                // "who asked for a test and where did it go" answerable.
                ["Recipient"] = email,
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new SendTestNotificationResult.Success(email);
    }
}
