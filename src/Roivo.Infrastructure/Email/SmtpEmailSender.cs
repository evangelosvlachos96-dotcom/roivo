using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Roivo.Infrastructure.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpSettings> settings, ILogger<SmtpEmailSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var builder = new BodyBuilder { HtmlBody = htmlBody };
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            var socketOptions = _settings.UseStartTls
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.Auto;

            await client.ConnectAsync(_settings.Host, _settings.Port, socketOptions, cancellationToken);
            await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient} with subject {Subject}", to, subject);
            throw;
        }
        finally
        {
            if (client.IsConnected)
            {
                try { await client.DisconnectAsync(true, cancellationToken); }
                catch (Exception ex) { _logger.LogWarning(ex, "SMTP disconnect failed"); }
            }
        }
    }
}
