using Hangfire;
using Microsoft.AspNetCore.HttpOverrides;
using MudBlazor.Services;
using Roivo.Aade.Configuration;
using Roivo.Aade.Jobs;
using Roivo.Application.Configuration;
using Roivo.Banking.Configuration;
using Roivo.Banking.Jobs;
using Roivo.Web.Components;
using Roivo.Web.Configuration;
using Roivo.Web.Configuration.Settings;
using Roivo.Web.Hangfire;
using Roivo.Infrastructure.Jobs;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Enable Banking hands out credentials as shell-style variables and .env is
// gitignored, so layer them in before anything binds EnableBankingSettings.
// Real environment variables still win — they are added after this.
builder.Configuration.AddDotEnvFile(
    DotEnvFile.FindNearest(builder.Environment.ContentRootPath) ?? ".env");
builder.Configuration.AddEnvironmentVariables();

builder.Host.UseRoivoLogging();

builder.Services
    .AddRoivoPersistence(builder.Configuration)
    .AddRoivoApplication()
    .AddRoivoBackgroundJobs(builder.Configuration)
    .AddRoivoAade(builder.Configuration)
    .AddRoivoBanking(builder.Configuration)
    .AddRoivoIdentity(builder.Configuration)
    .AddRoivoOpenIddict(builder.Configuration)
    .AddRoivoSecurity(builder.Configuration);

builder.Services.AddMudServices();
builder.Services.AddRazorPages();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Hangfire activates background jobs from the DI container; register here so
// the recurring registration below can resolve the type.
builder.Services.AddScoped<AadeFailureNotificationJob>();
builder.Services.AddScoped<BankingFailureNotificationJob>();

builder.Services.AddOptions<HangfireDashboardSettings>()
    .Bind(builder.Configuration.GetSection("Hangfire:Dashboard"));

var app = builder.Build();

// Render terminates TLS and forwards plain HTTP to the container, so without
// this the app sees http:// and OpenIddict rejects its own endpoints as
// insecure. Must run before anything that reads the scheme or client IP.
var forwardedHeaders = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
};

// KnownProxies/KnownNetworks default to loopback only, which silently drops the
// headers behind Render's proxy — its address is not fixed, and only the
// platform can reach the container, so trust the hop. Clearing is required:
// an empty collection initializer would leave the loopback defaults in place.
forwardedHeaders.KnownNetworks.Clear();
forwardedHeaders.KnownProxies.Clear();

app.UseForwardedHeaders(forwardedHeaders);

app.UseSerilogRequestLogging();
app.UseSecurityHeaders();
app.UseRoivoErrorHandling();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Mounted after UseAuthentication/UseAuthorization so the filter sees a
// populated User. Production leaves it unmounted entirely — the jobs still run,
// there is just no dashboard to reach.
if (app.Environment.IsDevelopment())
{
    // Dashboard is unauthenticated by default; only mount it in dev where
    // anything that could reach it is already a trusted developer.
    app.UseHangfireDashboard("/hangfire");
}
else if (app.Environment.IsStaging())
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new HangfireAuthorizationFilter()],
    });
}

// Register the nightly AADE sync. 03:00 in Europe/Athens — off-peak for our
// users and aligned with AADE's own quieter window.
RecurringJob.AddOrUpdate<NightlyAadeSyncJob>(
    "nightly-aade-sync",
    job => job.Execute(),
    Cron.Daily(3),
    new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Athens") });

// 09:00 Athens: notify owners whose AADE connection has been broken for >24h.
// Daytime delivery so the email lands when users are starting their day.
RecurringJob.AddOrUpdate<AadeFailureNotificationJob>(
    "aade-failure-notification",
    job => job.Execute(CancellationToken.None),
    Cron.Daily(9),
    new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Athens") });

// 04:00 Athens: an hour after the AADE sync so the two don't contend for the
// same Hangfire workers.
RecurringJob.AddOrUpdate<NightlyBankingSyncJob>(
    "nightly-banking-sync",
    job => job.Execute(),
    Cron.Daily(4),
    new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Athens") });

// 09:00 Athens: notify owners whose bank connection has been broken for >24h.
RecurringJob.AddOrUpdate<BankingFailureNotificationJob>(
    "banking-failure-notification",
    job => job.Execute(CancellationToken.None),
    Cron.Daily(9),
    new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Athens") });

// Keep-alive target for cron-job.org so Render's free tier does not idle the
// instance. Deliberately touches neither the database nor any service, so a
// cold or unhealthy dependency cannot make the ping fail and spin up alerts.
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("Health")
    .AllowAnonymous();

app.MapRazorPages();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
