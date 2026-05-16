using Microsoft.EntityFrameworkCore;
using Roivo.Infrastructure.Auditing;
using Roivo.Infrastructure.Email;
using Roivo.Infrastructure.MultiTenancy;
using Roivo.Infrastructure.Persistence;

namespace Roivo.Web.Configuration;

public static class PersistenceExtensions
{
    public static IServiceCollection AddRoivoPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, HttpTenantContext>();

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Default"));
            options.UseOpenIddict<Guid>();
        });

        services.AddOptions<SmtpSettings>()
            .Bind(configuration.GetSection("Smtp"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        services.AddScoped<IAuditService, AuditService>();

        return services;
    }
}
