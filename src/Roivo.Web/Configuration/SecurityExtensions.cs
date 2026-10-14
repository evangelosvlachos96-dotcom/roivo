using Microsoft.AspNetCore.DataProtection;
using Roivo.Infrastructure.Persistence;
using Roivo.Web.Configuration.Settings;

namespace Roivo.Web.Configuration;

public static class SecurityExtensions
{
    public static IServiceCollection AddRoivoSecurity(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);

        services.AddOptions<AuthCookieSettings>()
            .Bind(configuration.GetSection("AuthCookie"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<AppSettings>()
            .Bind(configuration.GetSection("App"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var cookie = configuration.GetSection("AuthCookie").Get<AuthCookieSettings>()
            ?? throw new InvalidOperationException("Missing 'AuthCookie' configuration section.");

        var app = configuration.GetSection("App").Get<AppSettings>()
            ?? throw new InvalidOperationException("Missing 'App' configuration section.");

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = cookie.LoginPath;
            options.LogoutPath = cookie.LogoutPath;
            options.AccessDeniedPath = cookie.AccessDeniedPath;
            options.ExpireTimeSpan = TimeSpan.FromDays(cookie.ExpirationDays);
            options.SlidingExpiration = true;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            // Strict everywhere but staging. A Strict cookie is withheld inside a
            // third-party frame, so a design tool embedding staging would only
            // ever reach the sign-in page. None is the only value a browser sends
            // in that context; it is paired with Secure=Always above. Production
            // keeps Strict — this is the CSRF defence for real accounts.
            options.Cookie.SameSite = environment.IsStaging()
                ? SameSiteMode.None
                : SameSiteMode.Strict;
            options.Cookie.Name = cookie.Name;
        });

        services.AddDataProtection()
            .PersistKeysToDbContext<ApplicationDbContext>()
            .SetApplicationName(app.Name);

        return services;
    }

    /// <summary>
    /// Host the staging framing relaxation applies to. Matched per request, so a
    /// misset ASPNETCORE_ENVIRONMENT cannot open framing on the production host.
    /// </summary>
    private const string StagingHost = "staging.roivo.gr";

    /// <summary>
    /// Frame-ancestors staging accepts. Figma's design tool renders the page
    /// inside its own origin, so framing must be permitted for it to see
    /// anything other than a blocked frame.
    /// </summary>
    private const string StagingFrameAncestors =
        "frame-ancestors 'self' https://*.figma.com https://figma.com";

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var isStaging = app.ApplicationServices
            .GetRequiredService<IWebHostEnvironment>()
            .IsStaging();

        return app.Use(async (context, next) =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // Framing stays denied everywhere but the staging host.
            var allowFraming = isStaging
                && context.Request.Host.Host.Equals(StagingHost, StringComparison.OrdinalIgnoreCase);

            if (allowFraming)
            {
                // X-Frame-Options has no "allow these origins" form — any value
                // at all blocks Figma — so the allowlist is expressed as CSP
                // frame-ancestors, which supersedes X-Frame-Options in browsers
                // that support both. The header is omitted rather than relaxed.
                context.Response.Headers["Content-Security-Policy"] = StagingFrameAncestors;
            }
            else
            {
                context.Response.Headers["X-Frame-Options"] = "DENY";
            }

            await next();
        });
    }

    public static WebApplication UseRoivoErrorHandling(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }
        return app;
    }
}
