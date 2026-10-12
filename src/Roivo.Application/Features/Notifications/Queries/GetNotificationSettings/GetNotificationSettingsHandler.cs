using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Features.Notifications.Queries.GetNotificationSettings;

/// <summary>
/// Reads a user's notification preferences for a business, falling back to the
/// defaults for their tenant type when they have never saved any.
/// </summary>
public sealed class GetNotificationSettingsHandler
{
    private readonly INotificationSettingsRepository _repository;
    private readonly ITenantContext _tenant;

    public GetNotificationSettingsHandler(
        INotificationSettingsRepository repository,
        ITenantContext tenant)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(tenant);

        _repository = repository;
        _tenant = tenant;
    }

    public async Task<NotificationSettingsView> Handle(
        GetNotificationSettingsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var stored = await _repository
            .GetAsync(query.BusinessId, query.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (stored is not null)
            return NotificationSettingsView.From(stored, isPersisted: true);

        // Nothing saved yet. Returning the defaults unpersisted — rather than
        // writing a row on first view — keeps a page load free of side effects
        // and means the defaults can change later without stale rows pinning
        // every user to the old ones.
        var defaults = NotificationSettings.CreateDefault(
            query.BusinessId,
            query.UserId,
            isAccountant: _tenant.CurrentTenantType == TenantType.Accountant);

        return NotificationSettingsView.From(defaults, isPersisted: false);
    }
}
