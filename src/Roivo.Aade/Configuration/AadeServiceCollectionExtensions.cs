using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using Roivo.Aade.Jobs;
using Roivo.Application.Abstractions.Aade;

namespace Roivo.Aade.Configuration;

public static class AadeServiceCollectionExtensions
{
    public static IServiceCollection AddRoivoAade(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<AadeSettings>()
            .Bind(configuration.GetSection("Aade"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var settings = configuration.GetSection("Aade").Get<AadeSettings>()
            ?? throw new InvalidOperationException("Missing 'Aade' configuration section.");

        services.AddHttpClient<IAadeClient, AadeHttpClient>(client =>
        {
            client.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            // Separate connect-timeout so DNS / TCP failures surface in
            // ~5s instead of waiting out the full request timeout.
            ConnectTimeout = TimeSpan.FromSeconds(settings.ConnectTimeoutSeconds),
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        })
        .AddPolicyHandler(GetRetryPolicy(settings.MaxRetries));

        services.AddScoped<IAadeCredentialStore, EncryptedCredentialStore>();

        services.AddScoped<NightlyAadeSyncJob>();
        services.AddScoped<SyncSingleBusinessJob>();

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int maxRetries)
    {
        // Retries: only on network failures and 5xx; explicitly NOT on 401/403
        // (credential errors should surface immediately, not retry-and-eventually-fail).
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                maxRetries,
                attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
    }
}
