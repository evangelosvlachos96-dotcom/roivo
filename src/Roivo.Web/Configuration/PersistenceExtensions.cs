using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;
using Roivo.Application.Abstractions;
using Roivo.Infrastructure.Auditing;
using Roivo.Application.Features.Notifications;
using Roivo.Infrastructure.Email;
using Roivo.Infrastructure.Email.Templates;
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

        // Render blocks outbound SMTP (25/465/587), so a deployed instance can
        // only deliver over HTTPS. The SMTP transport stays available for a
        // local sandbox mail server. The default lives on SmtpSettings so there
        // is one source of truth rather than two literals to keep in step.
        var transport = configuration.GetSection("Smtp")
            .GetValue("Transport", SmtpSettings.DefaultTransport);

        if (transport == EmailTransport.Smtp)
        {
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            services.AddHttpClient<IEmailSender, ResendEmailSender>(client =>
            {
                client.BaseAddress = new Uri("https://api.resend.com/");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            // IHttpClientFactory's logging handler redacts nothing by default, so
            // at Trace level the bearer token would land in the log file.
            .RedactLoggedHeaders(["Authorization"])
            .AddPolicyHandler(GetEmailRetryPolicy());
        }

        services.AddScoped<IBusinessRepository, BusinessRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IIncomeBookEntryRepository, IncomeBookEntryRepository>();
        services.AddScoped<IInvoiceQueryRepository, InvoiceQueryRepository>();
        services.AddScoped<IBankAccountRepository, BankAccountRepository>();
        services.AddScoped<IBankTransactionRepository, BankTransactionRepository>();
        services.AddScoped<IReconciliationRepository, ReconciliationRepository>();
        services.AddScoped<ICashflowRepository, CashflowRepository>();
        services.AddScoped<INotificationSettingsRepository, NotificationSettingsRepository>();

        // The dispatcher renders a template and hands it to IEmailSender; the
        // log is what stops a job re-sending the same notification every run.
        services.AddScoped<INotificationEmailDispatcher, NotificationEmailDispatcher>();
        services.AddScoped<INotificationDispatchLog, NotificationDispatchLog>();
        services.AddScoped<NotificationEmailRenderer>();
        services.AddScoped<IAuditWriter, AuditWriter>();

        return services;
    }

    /// <summary>
    /// Transient failures only — network errors, 5xx and 429. Never other 4xx:
    /// an unverified sending domain or a bad API key will not fix itself, and
    /// retrying only delays the error and burns Hangfire attempts.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetEmailRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(response => response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
    }
}
