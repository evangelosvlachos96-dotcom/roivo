using Microsoft.Extensions.Logging;

namespace Roivo.Infrastructure.Email;

/// <summary>
/// Hangfire-invoked wrapper around <see cref="IEmailSender"/>. The job server
/// activates this type per execution, so each run gets its own scoped dependencies.
/// </summary>
public sealed class EmailJob
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<EmailJob> _logger;

    public EmailJob(IEmailSender emailSender, ILogger<EmailJob> logger)
    {
        ArgumentNullException.ThrowIfNull(emailSender);
        ArgumentNullException.ThrowIfNull(logger);

        _emailSender = emailSender;
        _logger = logger;
    }

    // Hangfire invokes this by name on a background worker. Method must be
    // public and its argument types must be JSON-serializable so Hangfire can
    // persist them and replay on retry.
    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        try
        {
            await _emailSender.SendEmailAsync(to, subject, htmlBody);
        }
        catch (Exception ex)
        {
            // Re-throw so Hangfire's retry policy kicks in.
            _logger.LogError(ex, "Failed to send email to {Recipient}. Hangfire will retry.", to);
            throw;
        }
    }
}
