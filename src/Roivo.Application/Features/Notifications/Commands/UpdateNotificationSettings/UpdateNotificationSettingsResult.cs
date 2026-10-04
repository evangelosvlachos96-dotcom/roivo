namespace Roivo.Application.Features.Notifications.Commands.UpdateNotificationSettings;

/// <summary>Outcomes of <see cref="UpdateNotificationSettingsHandler"/>.</summary>
public abstract record UpdateNotificationSettingsResult
{
    /// <param name="Settings">The preferences as stored, so the caller can re-render from reality.</param>
    public sealed record Success(NotificationSettingsView Settings) : UpdateNotificationSettingsResult;

    public sealed record Forbidden : UpdateNotificationSettingsResult;

    public sealed record NotFound : UpdateNotificationSettingsResult;

    /// <param name="Reason">Diagnostic text from the domain. The UI chooses its own wording.</param>
    public sealed record Invalid(string Reason) : UpdateNotificationSettingsResult;
}
