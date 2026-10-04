namespace Roivo.Application.Features.Notifications.Queries.GetNotificationSettings;

/// <param name="UserId">Identity user id, as it appears on the principal's name-identifier claim.</param>
public sealed record GetNotificationSettingsQuery(Guid BusinessId, string UserId);
