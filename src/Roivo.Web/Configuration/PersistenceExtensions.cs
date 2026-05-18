using Microsoft.EntityFrameworkCore;
using Roivo.Application.Abstractions;
using Roivo.Infrastructure.Auditing;
using Roivo.Infrastructure.Email;
using Roivo.Infrastructure.MultiTenancy;
using Roivo.Infrastructure.Persistence;
using Roivo.Infrastructure.Persistence.Repositories;

namespace Roivo.Web.Configuration;

public static class PersistenceExtensions
{
    public static IServiceCollection AddRoivoPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, HttpTenantContext>();

        services.AddDbContextFactory<ApplicationDbContext>(
        (sp, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Default"));
            options.UseOpenIddict<Guid>();
            options.UseApplicationServiceProvider(sp);
        },
        lifetime: ServiceLifetime.Scoped);

        services.AddScoped<ApplicationDbContext>(sp =>
            sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>()
                .CreateDbContext());

        services.AddOptions<SmtpSettings>()
            .Bind(configuration.GetSection("Smtp"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        services.AddScoped<IBusinessRepository, BusinessRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IAuditWriter, AuditWriter>();

        return services;
    }
}
