using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using Roivo.Application.Abstractions.Banking;
using Roivo.Banking.Jobs;

namespace Roivo.Banking.Configuration;

public static class EnableBankingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Enable Banking client, the encrypted credential store and
    /// the sync jobs. Binds <see cref="EnableBankingSettings"/> from the
    /// "EnableBanking" configuration section — the host is expected to have
    /// layered <c>.env</c> values in via
    /// <see cref="DotEnvFile.AddDotEnvFile"/> before calling this.
    /// </summary>
    public static IServiceCollection AddRoivoBanking(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<EnableBankingSettings>()
            .Bind(configuration.GetSection("EnableBanking"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var settings = configuration.GetSection("EnableBanking").Get<EnableBankingSettings>()
            ?? throw new InvalidOperationException("Missing 'EnableBanking' configuration section.");

        // Singleton: it holds the imported RSA key, which is expensive to parse
        // and safe to share (RSA.SignData is thread-safe).
        services.AddSingleton<EnableBankingJwtFactory>();

        services.AddHttpClient<IBankingClient, EnableBankingClient>(client =>
        {
            client.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            // Separate connect-timeout so DNS / TCP failures surface in ~5s
            // instead of waiting out the full request timeout.
            ConnectTimeout = TimeSpan.FromSeconds(settings.ConnectTimeoutSeconds),
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        })
        .AddPolicyHandler(GetRetryPolicy(settings.MaxRetries));

        services.AddScoped<IBankingCredentialStore, EncryptedBankingCredentialStore>();
        services.AddSingleton<IBankingConnectionOptions, EnableBankingConnectionOptions>();

        services.AddScoped<NightlyBankingSyncJob>();
        services.AddScoped<SyncSingleBusinessBankingJob>();

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int maxRetries)
    {
        // Network failures, 5xx and 429 only. Never 401/403: a rejected JWT or a
        // lapsed consent is not going to fix itself, and retrying just delays
        // the error the user needs to see.
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(response => response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                maxRetries,
                attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
    }
}
