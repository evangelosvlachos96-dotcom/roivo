using Hangfire;
using Hangfire.PostgreSql;
using Roivo.Infrastructure.Email;

namespace Roivo.Web.Configuration;

public static class BackgroundJobsExtensions
{
    /// <summary>
    /// Wires Hangfire (PostgreSQL storage) and registers job types. Hangfire's
    /// own tables are created on first run against the configured connection.
    /// </summary>
    public static IServiceCollection AddRoivoBackgroundJobs(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing 'Default' connection string.");

        services.AddHangfire(config =>
        {
            config.UsePostgreSqlStorage(opt => opt.UseNpgsqlConnection(connectionString));
        });
        services.AddHangfireServer();

        services.AddScoped<EmailJob>();

        return services;
    }
}
