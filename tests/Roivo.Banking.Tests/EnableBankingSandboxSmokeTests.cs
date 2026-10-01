using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Roivo.Application.Abstractions.Banking.Results;
using Roivo.Banking;
using Roivo.Banking.Configuration;

namespace Roivo.Banking.Tests;

/// <summary>
/// Hits the real Enable Banking API using the credentials in the repository's
/// <c>.env</c>. Opt-in: set <c>ROIVO_BANKING_SMOKE=1</c> to run it. Live tests
/// fail for reasons outside the code (no network, an unactivated application,
/// an expired consent), so it stays out of the normal suite — but it is the
/// only check that proves Enable Banking actually accepts our JWT.
/// </summary>
[Trait("Category", "Smoke")]
public sealed class EnableBankingSandboxSmokeTests
{
    [SkippableFact]
    public async Task ListProviders_ReturnsGreekBanks()
    {
        Skip.IfNot(
            Environment.GetEnvironmentVariable("ROIVO_BANKING_SMOKE") == "1",
            "Live API test. Set ROIVO_BANKING_SMOKE=1 to run it.");

        var settings = TryLoadSettings();
        Skip.If(settings is null, "No Enable Banking credentials found — set them in .env to run this smoke test.");

        using var jwt = new EnableBankingJwtFactory(Options.Create(settings!));
        using var http = new HttpClient
        {
            BaseAddress = new Uri(settings!.BaseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds),
        };

        var client = new EnableBankingClient(http, jwt, NullLogger<EnableBankingClient>.Instance, Options.Create(settings));

        var result = await client.ListProvidersAsync(settings.Country, CancellationToken.None);

        result.Should().BeOfType<BankingProvidersResult.Success>(
            "a rejected JWT or an unregistered application id comes back as Unauthorized — the failures this test exists to catch. Actual: {0}",
            result);

        var providers = ((BankingProvidersResult.Success)result).Providers;
        providers.Should().NotBeEmpty();
        providers.Should().OnlyContain(p => p.Country == settings.Country);
    }

    /// <summary>
    /// Builds settings from the repository <c>.env</c>, or returns null when the
    /// file, the application id or the private key is missing.
    /// </summary>
    private static EnableBankingSettings? TryLoadSettings()
    {
        var envPath = DotEnvFile.FindNearest(AppContext.BaseDirectory);
        if (envPath is null)
            return null;

        var configuration = new ConfigurationBuilder()
            .AddDotEnvFile(envPath)
            .AddEnvironmentVariables()
            .Build();

        var applicationId = configuration["EnableBanking:ApplicationId"];
        var keyPath = configuration["EnableBanking:PrivateKeyPath"];
        if (string.IsNullOrWhiteSpace(applicationId) || string.IsNullOrWhiteSpace(keyPath))
            return null;

        // .env holds the key path relative to the repository root, which is
        // where .env itself lives — not the test's bin directory.
        var repositoryRoot = Path.GetDirectoryName(envPath)!;
        var resolvedKeyPath = Path.IsPathRooted(keyPath)
            ? keyPath
            : Path.GetFullPath(keyPath, repositoryRoot);

        if (!File.Exists(resolvedKeyPath))
            return null;

        return new EnableBankingSettings
        {
            BaseUrl = configuration["EnableBanking:BaseUrl"] ?? "https://api.enablebanking.com",
            ApplicationId = applicationId,
            PrivateKeyPath = resolvedKeyPath,
            RedirectUrl = configuration["EnableBanking:RedirectUrl"] ?? "https://localhost:7001/banking/callback",
        };
    }
}
