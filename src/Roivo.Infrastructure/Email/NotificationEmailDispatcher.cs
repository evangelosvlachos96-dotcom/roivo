using Microsoft.Extensions.Logging;
using Roivo.Application.Features.Notifications;
using Roivo.Core.Domain.Enums;
using Roivo.Infrastructure.Email.Templates;

namespace Roivo.Infrastructure.Email;

/// <summary>
/// Bridges the Application layer's <see cref="INotificationEmailDispatcher"/> to
/// the templates and the transport, both of which live in this project.
/// </summary>
public sealed class NotificationEmailDispatcher : INotificationEmailDispatcher
{
    private readonly NotificationEmailRenderer _renderer;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<NotificationEmailDispatcher> _logger;

    public NotificationEmailDispatcher(
        NotificationEmailRenderer renderer,
        IEmailSender emailSender,
        ILogger<NotificationEmailDispatcher> logger)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(emailSender);
        ArgumentNullException.ThrowIfNull(logger);

        _renderer = renderer;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task SendSampleAsync(
        NotificationKind kind,
        string recipientEmail,
        Guid businessId,
        string businessName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientEmail);

        var email = _renderer.RenderSample(kind, businessId, businessName);

        // Sent inline rather than through EmailJob: the user is watching the
        // settings page for a result, and a queued send would report success
        // before the provider had accepted anything.
        await _emailSender
            .SendEmailAsync(recipientEmail, email.Subject, email.HtmlBody, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Sent {Kind} test notification to {Recipient} for business {BusinessId}",
            kind, recipientEmail, businessId);
    }
}
