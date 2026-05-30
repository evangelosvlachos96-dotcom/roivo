using Roivo.Infrastructure.Email;

namespace Roivo.Infrastructure.Tests.Fakes;

public sealed record SentEmail(string To, string Subject, string Body);

public sealed class RecordingEmailSender : IEmailSender
{
    public List<SentEmail> Sent { get; } = new();
    public Exception? NextException { get; set; }

    public Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (NextException is not null)
        {
            var ex = NextException;
            NextException = null;
            throw ex;
        }
        Sent.Add(new SentEmail(to, subject, htmlBody));
        return Task.CompletedTask;
    }
}
