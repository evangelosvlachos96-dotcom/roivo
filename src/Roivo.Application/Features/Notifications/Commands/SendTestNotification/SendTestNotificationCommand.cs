using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Features.Notifications.Commands.SendTestNotification;

/// <summary>
/// Asks for one sample email of <paramref name="Kind"/>.
/// </summary>
/// <remarks>
/// Carries no recipient address on purpose: the handler resolves the user's own
/// confirmed address from the account. Accepting an address from the caller
/// would turn the settings page into an open relay for sending Roivo-branded
/// mail anywhere.
/// </remarks>
public sealed record SendTestNotificationCommand(
    Guid BusinessId,
    string UserId,
    NotificationKind Kind);
