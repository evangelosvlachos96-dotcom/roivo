using Microsoft.AspNetCore.DataProtection;
using Roivo.Infrastructure.Persistence;
using Roivo.Web.Configuration.Settings;

namespace Roivo.Web.Configuration;

public static class SecurityExtensions
{
    public static IServiceCollection AddRoivoSecurity(this IServiceCollection services, IConfiguration configuration)
    {
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
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.Name = cookie.Name;
        });

        services.AddDataProtection()
            .PersistKeysToDbContext<ApplicationDbContext>()
            .SetApplicationName(app.Name);

        return services;
    }

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
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
