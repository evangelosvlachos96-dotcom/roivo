using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Roivo.Application.Abstractions.Banking.Results;
using Roivo.Banking;
using Roivo.Banking.Configuration;

namespace Roivo.Banking.Tests;

/// <summary>
/// Pins how API failures are reported. Every failure comes back as a result type
/// rather than an exception, so if the API's own explanation is dropped here it
/// is gone for good — the UI only ever shows a generic localised message.
/// </summary>
public sealed class EnableBankingClientErrorReportingTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("roivo-eb-err-").FullName;
    private readonly System.Security.Cryptography.RSA _key = System.Security.Cryptography.RSA.Create(2048);

    [Fact]
    public async Task SurfacesTheApiMessageWhenTheErrorCodeIsAJsonNumber()
    {
        // Enable Banking sends {"code":403,...} as a bare number. Binding that to
        // a string made the whole envelope fail to parse, which is how a precise
        // "Application does not exist" turned into an unexplained failure.
        var (client, _) = CreateClient(
            HttpStatusCode.Forbidden,
            """{"code":403,"message":"Application does not exist"}""");

        var result = await client.ListProvidersAsync("GR", CancellationToken.None);

        result.Should().BeOfType<BankingProvidersResult.Unauthorized>()
            .Which.Message.Should().Be("403: Application does not exist");
    }

    [Fact]
    public async Task SurfacesTheApiMessageWhenTheErrorCodeIsAString()
    {
        var (client, _) = CreateClient(
            HttpStatusCode.BadRequest,
            """{"code":"INVALID_REQUEST","message":"Bad aspsp"}""");

        var result = await client.ListProvidersAsync("GR", CancellationToken.None);

        result.Should().BeOfType<BankingProvidersResult.BankingServerError>()
            .Which.Message.Should().Be("INVALID_REQUEST: Bad aspsp");
    }

    [Fact]
    public async Task SurfacesTheMessageWhenThereIsNoCodeAtAll()
    {
        var (client, _) = CreateClient(HttpStatusCode.BadRequest, """{"message":"Something broke"}""");

        var result = await client.ListProvidersAsync("GR", CancellationToken.None);

        result.Should().BeOfType<BankingProvidersResult.BankingServerError>()
            .Which.Message.Should().Be("Something broke");
    }

    [Fact]
    public async Task FallsBackToTheRawBodyWhenTheErrorIsNotJson()
    {
        var (client, _) = CreateClient(HttpStatusCode.BadGateway, "<html>502 Bad Gateway</html>");

        var result = await client.ListProvidersAsync("GR", CancellationToken.None);

        result.Should().BeOfType<BankingProvidersResult.BankingServerError>()
            .Which.Message.Should().Contain("502 Bad Gateway");
    }

    [Fact]
    public async Task FallsBackToTheReasonPhraseForAnEmptyBody()
    {
        var (client, _) = CreateClient(HttpStatusCode.ServiceUnavailable, "");

        var result = await client.ListProvidersAsync("GR", CancellationToken.None);

        result.Should().BeOfType<BankingProvidersResult.BankingServerError>()
            .Which.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task LogsTheFailureWithTheStageAndStatusSoTheLogIsActionable()
    {
        var (client, logger) = CreateClient(
            HttpStatusCode.Forbidden,
            """{"code":403,"message":"Application does not exist"}""");

        await client.ListProvidersAsync("GR", CancellationToken.None);

        var line = logger.Warnings.Should().ContainSingle().Subject;
        line.Should().Contain("list providers");
        line.Should().Contain("403");
        line.Should().Contain("Application does not exist");
    }

    [Fact]
    public async Task SendsABearerTokenOnEveryRequest()
    {
        var (client, _) = CreateClient(HttpStatusCode.OK, """{"aspsps":[]}""", out var handler);

        await client.ListProvidersAsync("GR", CancellationToken.None);

        handler.LastRequest!.Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.LastRequest.Headers.Authorization.Parameter.Should().NotBeNullOrWhiteSpace();
    }

    private (EnableBankingClient Client, CollectingLogger Logger) CreateClient(HttpStatusCode status, string body)
        => CreateClient(status, body, out _);

    private (EnableBankingClient Client, CollectingLogger Logger) CreateClient(
        HttpStatusCode status, string body, out StubHandler handler)
    {
        handler = new StubHandler(status, body);
        var keyPath = Path.Combine(_directory, "key.pem");
        File.WriteAllText(keyPath, _key.ExportPkcs8PrivateKeyPem());

        var settings = Options.Create(new EnableBankingSettings
        {
            BaseUrl = "https://api.enablebanking.com",
            ApplicationId = "app-123",
            PrivateKeyPath = keyPath,
            RedirectUrl = "https://localhost/banking/callback",
        });

        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.enablebanking.com/") };
        var logger = new CollectingLogger();

        // The factory holds the RSA key; the client outlives this call in the
        // test, and the process exit reclaims it.
        var jwt = new EnableBankingJwtFactory(settings);

        return (new EnableBankingClient(http, jwt, logger, settings), logger);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;

        public HttpRequestMessage? LastRequest { get; private set; }

        public StubHandler(HttpStatusCode status, string body)
        {
            _status = status;
            _body = body;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json"),
                RequestMessage = request,
            });
        }
    }

    /// <summary>Captures warning-level messages so a test can assert on them.</summary>
    private sealed class CollectingLogger : ILogger<EnableBankingClient>
    {
        public List<string> Warnings { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
                Warnings.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }

    public void Dispose()
    {
        _key.Dispose();
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }
}
