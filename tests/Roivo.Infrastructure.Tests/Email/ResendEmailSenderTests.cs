using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Roivo.Infrastructure.Email;

namespace Roivo.Infrastructure.Tests.Email;

public class ResendEmailSenderTests
{
    private const string ApiKey = "re_test_key";

    /// <summary>Captures the outgoing request and replies with a programmed response.</summary>
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
        public string ResponseBody { get; set; } = """{"id":"abc-123"}""";

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // A real handler observes the token; this fake must too, or a test
            // for cancellation would pass regardless of whether the sender
            // actually threads it through.
            cancellationToken.ThrowIfCancellationRequested();

            Request = request;
            Body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(Status)
            {
                Content = new StringContent(ResponseBody, Encoding.UTF8, "application/json"),
            };
        }
    }

    private static (ResendEmailSender Sender, CapturingHandler Handler) BuildSut(
        string fromName = "Roivo", string fromEmail = "noreply@roivo.gr")
    {
        var handler = new CapturingHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.resend.com/") };

        var settings = Options.Create(new SmtpSettings
        {
            Host = "smtp.resend.com",
            Port = 587,
            Username = "resend",
            Password = ApiKey,
            FromEmail = fromEmail,
            FromName = fromName,
            UseStartTls = true,
        });

        return (new ResendEmailSender(http, settings, NullLogger<ResendEmailSender>.Instance), handler);
    }

    [Fact]
    public async Task Posts_to_the_send_endpoint_over_https()
    {
        var (sender, handler) = BuildSut();

        await sender.SendEmailAsync("someone@example.com", "Subject", "<p>Body</p>");

        handler.Request!.Method.Should().Be(HttpMethod.Post);
        handler.Request.RequestUri!.ToString().Should().Be("https://api.resend.com/emails");

        // Port 443, not 587 — the whole point of this transport.
        handler.Request.RequestUri.Scheme.Should().Be("https");
        handler.Request.RequestUri.Port.Should().Be(443);
    }

    [Fact]
    public async Task Sends_the_api_key_as_a_bearer_token()
    {
        var (sender, handler) = BuildSut();

        await sender.SendEmailAsync("someone@example.com", "Subject", "<p>Body</p>");

        handler.Request!.Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.Request.Headers.Authorization.Parameter.Should().Be(ApiKey);
    }

    [Fact]
    public async Task Builds_the_payload_resend_expects()
    {
        var (sender, handler) = BuildSut();

        await sender.SendEmailAsync("someone@example.com", "Επιβεβαίωση", "<p>Γειά</p>");

        using var document = JsonDocument.Parse(handler.Body!);
        var root = document.RootElement;

        root.GetProperty("from").GetString().Should().Be("Roivo <noreply@roivo.gr>");
        root.GetProperty("to").EnumerateArray().Select(e => e.GetString())
            .Should().ContainSingle().Which.Should().Be("someone@example.com");
        root.GetProperty("subject").GetString().Should().Be("Επιβεβαίωση");
        root.GetProperty("html").GetString().Should().Be("<p>Γειά</p>");
    }

    [Fact]
    public async Task Omits_the_display_name_when_unset()
    {
        var (sender, handler) = BuildSut(fromName: " ");

        await sender.SendEmailAsync("someone@example.com", "Subject", "<p>Body</p>");

        using var document = JsonDocument.Parse(handler.Body!);
        document.RootElement.GetProperty("from").GetString().Should().Be("noreply@roivo.gr");
    }

    [Fact]
    public async Task Throws_when_resend_rejects_the_request()
    {
        var (sender, handler) = BuildSut();
        handler.Status = HttpStatusCode.Forbidden;
        handler.ResponseBody = """{"message":"The roivo.gr domain is not verified."}""";

        var act = () => sender.SendEmailAsync("someone@example.com", "Subject", "<p>Body</p>");

        // Throwing matters: EmailJob relies on it to trigger Hangfire's retry.
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*403*someone@example.com*");
    }

    [Fact]
    public async Task Throws_when_no_api_key_is_configured()
    {
        var handler = new CapturingHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.resend.com/") };
        var settings = Options.Create(new SmtpSettings
        {
            Host = "smtp.resend.com",
            Port = 587,
            Username = "resend",
            Password = "   ",
            FromEmail = "noreply@roivo.gr",
            FromName = "Roivo",
            UseStartTls = true,
        });

        var sender = new ResendEmailSender(http, settings, NullLogger<ResendEmailSender>.Instance);

        var act = () => sender.SendEmailAsync("someone@example.com", "Subject", "<p>Body</p>");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*API key*");
        handler.Request.Should().BeNull("nothing should go out without a key");
    }

    [Fact]
    public async Task Rejects_an_empty_recipient()
    {
        var (sender, handler) = BuildSut();

        var act = () => sender.SendEmailAsync("", "Subject", "<p>Body</p>");

        await act.Should().ThrowAsync<ArgumentException>();
        handler.Request.Should().BeNull();
    }

    [Fact]
    public void Defaults_to_the_resend_transport()
    {
        // Render blocks SMTP, so a deployment that sets no transport must still
        // get the one that works there.
        var settings = new SmtpSettings
        {
            Host = "smtp.resend.com",
            Port = 587,
            Username = "resend",
            Password = ApiKey,
            FromEmail = "noreply@roivo.gr",
            FromName = "Roivo",
            UseStartTls = true,
        };

        settings.Transport.Should().Be(EmailTransport.Resend);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Throws_on_rate_limit_and_server_errors(HttpStatusCode status)
    {
        var (sender, handler) = BuildSut();
        handler.Status = status;

        var act = () => sender.SendEmailAsync("someone@example.com", "Subject", "<p>Body</p>");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Does_not_leak_the_api_key_in_the_failure_message()
    {
        var (sender, handler) = BuildSut();
        handler.Status = HttpStatusCode.Unauthorized;
        handler.ResponseBody = """{"message":"API key is invalid."}""";

        var thrown = await sender.Invoking(x =>
                x.SendEmailAsync("someone@example.com", "Subject", "<p>Body</p>"))
            .Should().ThrowAsync<InvalidOperationException>();

        thrown.Which.Message.Should().NotContain(ApiKey);
        thrown.Which.ToString().Should().NotContain(ApiKey);
    }

    [Fact]
    public async Task Propagates_cancellation_without_completing_the_send()
    {
        var (sender, _) = BuildSut();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => sender.SendEmailAsync(
            "someone@example.com", "Subject", "<p>Body</p>", cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Quotes_a_display_name_containing_rfc_specials()
    {
        // MimeKit quoted these on the SMTP path; an unquoted comma produces a
        // malformed address that Resend reads as two recipients.
        var (sender, handler) = BuildSut(fromName: "Roivo, A.E.");

        await sender.SendEmailAsync("someone@example.com", "Subject", "<p>Body</p>");

        using var document = JsonDocument.Parse(handler.Body!);
        document.RootElement.GetProperty("from").GetString()
            .Should().Be("\"Roivo, A.E.\" <noreply@roivo.gr>");
    }

    [Fact]
    public void Default_transport_is_resend_from_a_single_source_of_truth()
    {
        // Render blocks SMTP, so a deployment that names no transport must still
        // get the one that works there. The DI registration reads this same
        // constant, so the two cannot drift apart.
        SmtpSettings.DefaultTransport.Should().Be(EmailTransport.Resend);
    }
}
