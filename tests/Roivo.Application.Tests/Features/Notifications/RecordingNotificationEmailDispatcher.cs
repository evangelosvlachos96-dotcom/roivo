using Roivo.Application.Features.Notifications;
using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Tests.Features.Notifications;

public sealed record SampleSend(NotificationKind Kind, string Recipient, Guid BusinessId, string BusinessName);

public sealed class RecordingNotificationEmailDispatcher : INotificationEmailDispatcher
{
    public List<SampleSend> Sends { get; } = new();

    /// <summary>Thrown once on the next send, then cleared.</summary>
    public Exception? NextException { get; set; }

    public Task SendSampleAsync(
        NotificationKind kind,
        string recipientEmail,
        Guid businessId,
        string businessName,
        CancellationToken cancellationToken = default)
    {
        if (NextException is not null)
        {
            var ex = NextException;
            NextException = null;
            throw ex;
        }

        Sends.Add(new SampleSend(kind, recipientEmail, businessId, businessName));
        return Task.CompletedTask;
    }
}
