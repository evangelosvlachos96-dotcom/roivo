using System.ComponentModel.DataAnnotations;

namespace Roivo.Infrastructure.Email;

public class SmtpSettings
{
    /// <summary>
    /// Transport used when configuration names none. Resend, because Render
    /// blocks the outbound SMTP ports and a deployment that forgets to set this
    /// must still be able to send.
    /// </summary>
    public const EmailTransport DefaultTransport = EmailTransport.Resend;

    /// <summary>
    /// Which transport delivers the mail. Defaults to
    /// <see cref="EmailTransport.Resend"/> because Render blocks the outbound
    /// SMTP ports, so a deployed instance can only reach the provider over
    /// HTTPS. Set to <see cref="EmailTransport.Smtp"/> for a local Mailtrap or
    /// MailHog box.
    /// </summary>
    public EmailTransport Transport { get; init; } = DefaultTransport;

    /// <summary>SMTP host. Unused by the Resend transport but still required so
    /// a single configuration shape serves both.</summary>
    [Required]
    public required string Host { get; init; }

    [Range(1, 65535)]
    public required int Port { get; init; }

    [Required]
    public required string Username { get; init; }

    [Required]
    public required string Password { get; init; }

    [Required, EmailAddress]
    public required string FromEmail { get; init; }

    [Required]
    public required string FromName { get; init; }

    public required bool UseStartTls { get; init; }
}
