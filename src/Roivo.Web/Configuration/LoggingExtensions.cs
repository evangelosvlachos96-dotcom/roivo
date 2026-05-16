using Roivo.Web.Configuration.Settings;
using Serilog;

namespace Roivo.Web.Configuration;

public static class LoggingExtensions
{
    public static IHostBuilder UseRoivoLogging(this IHostBuilder host)
    {
        return host.UseSerilog((context, services, configuration) =>
        {
            var app = context.Configuration.GetSection("App").Get<AppSettings>()
                ?? throw new InvalidOperationException("Missing 'App' configuration section.");

            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(app.LogFilePath, rollingInterval: RollingInterval.Day);
        });
    }
}
