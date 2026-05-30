using Microsoft.AspNetCore.Identity;
using Roivo.Core.Domain.Entities;
using Roivo.Infrastructure.Identity;
using Roivo.Infrastructure.Persistence;
using Roivo.Web.Configuration.Settings;

namespace Roivo.Web.Configuration;

public static class IdentityExtensions
{
    public static IServiceCollection AddRoivoIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<IdentitySettings>()
            .Bind(configuration.GetSection("Identity"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
         

        var settings = configuration.GetSection("Identity").Get<IdentitySettings>()
            ?? throw new InvalidOperationException("Missing 'Identity' configuration section.");

        services
            .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequiredLength = settings.Password.RequiredLength;
                options.Password.RequireDigit = settings.Password.RequireDigit;
                options.Password.RequireUppercase = settings.Password.RequireUppercase;
                options.Password.RequireLowercase = settings.Password.RequireLowercase;
                options.Password.RequireNonAlphanumeric = settings.Password.RequireNonAlphanumeric;
                options.Lockout.MaxFailedAccessAttempts = settings.Lockout.MaxFailedAccessAttempts;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(settings.Lockout.DefaultLockoutMinutes);
                options.SignIn.RequireConfirmedEmail = settings.SignIn.RequireConfirmedEmail;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // SecurityStampValidatorOptions.ValidationInterval is left at the default
        // 30 minutes. The defensive sign-out in MainLayout catches deleted users
        // on the next navigation, so the tight 1-minute interval (which made
        // Identity's scoped DbContext race with other queries) isn't needed.

        services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, TenantClaimsPrincipalFactory>();
        services.AddAuthorization();
        services.AddCascadingAuthenticationState();

        return services;
    }
}
