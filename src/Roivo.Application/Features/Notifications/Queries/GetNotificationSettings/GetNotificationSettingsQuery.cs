namespace Roivo.Application.Features.Notifications.Queries.GetNotificationSettings;

/// <summary>Reads one user's notification preferences for one business.</summary>
/// <param name="BusinessId">The business whose preferences to read.</param>
/// <param name="UserId">Identity user id, as it appears on the principal's name-identifier claim.</param>
public sealed record GetNotificationSettingsQuery(Guid BusinessId, string UserId);
