using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Features.Notifications;

/// <summary>
/// Renders and sends one notification email. Implemented in
/// <c>Roivo.Infrastructure</c>, which owns both the HTML templates and the
/// transport.
/// </summary>
/// <remarks>
/// The interface exists so the Application layer can trigger a send without
/// seeing <c>IEmailSender</c> or the Greek resource strings, neither of which it
/// is allowed to reference.
/// </remarks>
public interface INotificationEmailDispatcher
{
    /// <summary>
    /// Sends a sample of <paramref name="kind"/>, filled with placeholder
    /// figures, to <paramref name="recipientEmail"/>. Used by the settings page's
    /// "send test email" button so a user can see what they signed up for.
    /// </summary>
    /// <exception cref="Exception">
    /// Propagates transport failures; the caller turns them into a result variant.
    /// </exception>
    Task SendSampleAsync(
        NotificationKind kind,
        string recipientEmail,
        Guid businessId,
        string businessName,
        CancellationToken cancellationToken = default);
}
