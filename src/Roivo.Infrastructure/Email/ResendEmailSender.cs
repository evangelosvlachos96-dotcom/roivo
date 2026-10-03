using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Roivo.Infrastructure.Email;

/// <summary>
/// Sends mail through Resend's HTTPS API rather than SMTP.
/// </summary>
/// <remarks>
/// Render blocks outbound connections on the SMTP ports (25/465/587) on its
/// managed plans, so <see cref="SmtpEmailSender"/> cannot deliver from a
/// deployed instance — the connect attempt hangs until it times out, and every
/// confirmation and reset mail silently fails. Port 443 is not blocked, so the
/// provider's REST API is the only transport that works there.
/// </remarks>
public sealed class ResendEmailSender : IEmailSender
{
    /// <summary>Resend's send endpoint. Relative to the client's configured base address.</summary>
    private const string SendPath = "emails";

    private readonly HttpClient _http;
    private readonly SmtpSettings _settings;
    private readonly ILogger<ResendEmailSender> _logger;

    public ResendEmailSender(
        HttpClient http,
        IOptions<SmtpSettings> settings,
        ILogger<ResendEmailSender> logger)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(logger);

        _http = http;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(to);
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(htmlBody);

        var payload = new ResendSendRequest(
            From: FormatFrom(_settings.FromName, _settings.FromEmail),
            To: [to],
            Subject: subject,
            Html: htmlBody);

        using var request = new HttpRequestMessage(HttpMethod.Post, SendPath)
        {
            Content = JsonContent.Create(payload),
        };

        // The API key is read per request rather than set once on the client so
        // a rotated key takes effect without a restart.
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ResolveApiKey());

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Sent email to {Recipient} with subject {Subject}", to, subject);
            return;
        }

        // Resend reports the reason in the body (unverified domain, bad key,
        // rate limit). Without it the status alone is not actionable.
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogError(
            "Resend rejected the email to {Recipient} with subject {Subject}: {Status} {Body}",
            to, subject, (int)response.StatusCode, Truncate(body, 500));

        throw new InvalidOperationException(
            $"Resend returned {(int)response.StatusCode} when sending to '{to}'.");
    }

    /// <summary>
    /// The API key, taken from <see cref="SmtpSettings.Password"/>. Resend's own
    /// SMTP credentials use the API key as the password, so the one secret
    /// serves both transports and deployments need no new variable.
    /// </summary>
    private string ResolveApiKey()
    {
        if (string.IsNullOrWhiteSpace(_settings.Password))
            throw new InvalidOperationException("No Resend API key configured (Smtp:Password).");

        return _settings.Password;
    }

    /// <summary>
    /// Formats the sender as <c>Name &lt;address&gt;</c>, or bare when unnamed.
    /// A name containing RFC 5322 specials is quoted, which MailboxAddress used
    /// to handle on the SMTP path.
    /// </summary>
    private static string FormatFrom(string? name, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
            return email;

        var trimmed = name.Trim();

        return trimmed.AsSpan().IndexOfAny(SpecialNameCharacters) >= 0
            ? $"\"{trimmed.Replace("\\", "\\\\").Replace("\"", "\\\"")}\" <{email}>"
            : $"{trimmed} <{email}>";
    }

    /// <summary>Characters that force the display name to be a quoted string.</summary>
    private static readonly System.Buffers.SearchValues<char> SpecialNameCharacters =
        System.Buffers.SearchValues.Create("()<>[]:;@\\,.\"");

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max];

    private sealed record ResendSendRequest(
        [property: JsonPropertyName("from")] string From,
        [property: JsonPropertyName("to")] IReadOnlyList<string> To,
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("html")] string Html);
}
