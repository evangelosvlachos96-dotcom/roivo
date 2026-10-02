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

// Deployment environment variables, in readable single-underscore names.
//
// The built-in convention spells these CONNECTIONSTRINGS__DEFAULT and
// SMTP__FROMEMAIL: `__` is the section separator and a lone `_` is neither a
// separator nor ignored, so it cannot sit between words. These names are the
// deployment contract instead, mapped onto the keys the app actually reads.
// Mirrored in docs/CONFIGURATION.md — keep the two in step.
//
// Registered last, so a variable set here wins over appsettings.json, the .env
// file, and the `__` spelling of the same key.
var environmentKeyMap = new Dictionary<string, string>(StringComparer.Ordinal)
{
    ["DATABASE_CONNECTION_STRING"] = "ConnectionStrings:Default",

    // The same names DotEnvFile maps out of .env for local development, so one
    // spelling works whether it comes from the file or the environment.
    ["ENABLE_BANKING_BASE_URL"] = "EnableBanking:BaseUrl",
    ["ENABLE_BANKING_APPLICATION_ID"] = "EnableBanking:ApplicationId",
    ["ENABLE_BANKING_PRIVATE_KEY_PATH"] = "EnableBanking:PrivateKeyPath",
    ["ENABLE_BANKING_REDIRECT_URL"] = "EnableBanking:RedirectUrl",

    ["APP_PUBLIC_BASE_URL"] = "App:PublicBaseUrl",

    ["HANGFIRE_AUTHORIZED_EMAILS"] = "Hangfire:Dashboard:AuthorizedEmails",

    ["SMTP_HOST"] = "Smtp:Host",
    ["SMTP_PORT"] = "Smtp:Port",
    ["SMTP_USERNAME"] = "Smtp:Username",
    ["SMTP_PASSWORD"] = "Smtp:Password",
    ["SMTP_FROM_EMAIL"] = "Smtp:FromEmail",
    ["SMTP_FROM_NAME"] = "Smtp:FromName",
    ["SMTP_USE_STARTTLS"] = "Smtp:UseStartTls",
};

var mappedEnvironment = new Dictionary<string, string?>(StringComparer.Ordinal);
foreach (var (environmentName, configurationKey) in environmentKeyMap)
{
    var value = Environment.GetEnvironmentVariable(environmentName);
    if (!string.IsNullOrEmpty(value))
        mappedEnvironment[configurationKey] = value;
}

if (mappedEnvironment.Count > 0)
    builder.Configuration.AddInMemoryCollection(mappedEnvironment);

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
builder.Services.AddScoped<NightlyReconciliationJob>();
builder.Services.AddScoped<NightlyCashflowForecastJob>();

builder.Services.AddOptions<HangfireDashboardSettings>()
    .Bind(builder.Configuration.GetSection("Hangfire:Dashboard"));

// Swagger describes the API surface, which is useful while developing against
// staging but is not something production should advertise. Registered only
// where it is served, so Production carries neither the services nor the
// generated document.
if (builder.Environment.IsDevelopment() || builder.Environment.IsStaging())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

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

// Mirrors the registration above: Development and Staging only, never
// Production.
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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

// Resolved from DI rather than the static RecurringJob facade: that facade
// reads JobStorage.Current, which only gets set as a side effect of mounting
// the dashboard. Production mounts no dashboard, so the static calls threw
// "Current JobStorage instance has not been initialized yet" and the app never
// started. IRecurringJobManager takes its storage from the container.
var recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();

// Register the nightly AADE sync. 03:00 in Europe/Athens — off-peak for our
// users and aligned with AADE's own quieter window.
recurringJobs.AddOrUpdate<NightlyAadeSyncJob>(
    "nightly-aade-sync",
    job => job.Execute(),
    Cron.Daily(3),
    new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Athens") });

// 09:00 Athens: notify owners whose AADE connection has been broken for >24h.
// Daytime delivery so the email lands when users are starting their day.
recurringJobs.AddOrUpdate<AadeFailureNotificationJob>(
    "aade-failure-notification",
    job => job.Execute(CancellationToken.None),
    Cron.Daily(9),
    new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Athens") });

// 04:00 Athens: an hour after the AADE sync so the two don't contend for the
// same Hangfire workers.
recurringJobs.AddOrUpdate<NightlyBankingSyncJob>(
    "nightly-banking-sync",
    job => job.Execute(),
    Cron.Daily(4),
    new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Athens") });

// 09:00 Athens: notify owners whose bank connection has been broken for >24h.
recurringJobs.AddOrUpdate<BankingFailureNotificationJob>(
    "banking-failure-notification",
    job => job.Execute(CancellationToken.None),
    Cron.Daily(9),
    new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Athens") });

// 05:00 Athens: an hour after the banking sync, so the night's transactions are
// already imported and there is something new to match.
recurringJobs.AddOrUpdate<NightlyReconciliationJob>(
    "nightly-reconciliation",
    job => job.Execute(),
    Cron.Daily(5),
    new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Athens") });

// 06:00 Athens: after reconciliation, so the forecast is built on matched data.
recurringJobs.AddOrUpdate<NightlyCashflowForecastJob>(
    "nightly-cashflow-forecast",
    job => job.Execute(),
    Cron.Daily(6),
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
