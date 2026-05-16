using Roivo.Infrastructure.Persistence;
using Roivo.Web.Configuration.Settings;

namespace Roivo.Web.Configuration;

public static class OpenIddictExtensions
{
    public static IServiceCollection AddRoivoOpenIddict(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OpenIddictSettings>()
            .Bind(configuration.GetSection("OpenIddict"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var settings = configuration.GetSection("OpenIddict").Get<OpenIddictSettings>()
            ?? throw new InvalidOperationException("Missing 'OpenIddict' configuration section.");

        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                       .UseDbContext<ApplicationDbContext>()
                       .ReplaceDefaultEntities<Guid>();
            })
            .AddServer(options =>
            {
                options.SetAuthorizationEndpointUris(settings.AuthorizationEndpoint)
                       .SetTokenEndpointUris(settings.TokenEndpoint)
                       .SetUserInfoEndpointUris(settings.UserInfoEndpoint);

                options.AllowAuthorizationCodeFlow()
                       .AllowRefreshTokenFlow()
                       .RequireProofKeyForCodeExchange();

                options.AddDevelopmentEncryptionCertificate()
                       .AddDevelopmentSigningCertificate();

                options.SetAccessTokenLifetime(TimeSpan.FromMinutes(settings.AccessTokenLifetimeMinutes))
                       .SetRefreshTokenLifetime(TimeSpan.FromDays(settings.RefreshTokenLifetimeDays));

                options.UseAspNetCore()
                       .EnableAuthorizationEndpointPassthrough()
                       .EnableTokenEndpointPassthrough()
                       .EnableUserInfoEndpointPassthrough();
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        return services;
    }
}
