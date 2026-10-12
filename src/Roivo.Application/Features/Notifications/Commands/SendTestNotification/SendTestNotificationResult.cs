namespace Roivo.Application.Features.Notifications.Commands.SendTestNotification;

/// <summary>Outcomes of <see cref="SendTestNotificationHandler"/>.</summary>
public abstract record SendTestNotificationResult
{
    /// <summary>
    /// <see cref="SendFailed.Reason"/> when the account has no confirmed email
    /// address. A sentinel rather than prose so the UI can show its own wording
    /// for the one failure a user can actually fix.
    /// </summary>
    public const string NoConfirmedEmailReason = "NoConfirmedEmail";

    /// <param name="RecipientEmail">Where it went, so the UI can say so.</param>
    public sealed record Success(string RecipientEmail) : SendTestNotificationResult;

    public sealed record Forbidden : SendTestNotificationResult;

    public sealed record NotFound : SendTestNotificationResult;

    /// <param name="Reason">
    /// Transport diagnostic, or <see cref="NoConfirmedEmailReason"/>.
    /// </param>
    public sealed record SendFailed(string Reason) : SendTestNotificationResult;
}
