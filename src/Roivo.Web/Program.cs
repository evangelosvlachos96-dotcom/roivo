using Hangfire;
using Roivo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
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
using Roivo.Resources;
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
    // "Resend" (default, and the only transport that works on Render) or "Smtp".
    ["SMTP_TRANSPORT"] = "Smtp:Transport",
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
    .AddRoivoSecurity(builder.Configuration, builder.Environment);

builder.Services.AddMudServices();
builder.Services.AddRazorPages();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Hangfire activates background jobs from the DI container; register here so
// the recurring registration below can resolve the type.
builder.Services.AddScoped<AadeFailureNotificationJob>();
builder.Services.AddScoped<BankingFailureNotificationJob>();
builder.Services.AddScoped<NightlyReconciliationJob>();
builder.Services.AddScoped<NightlyCashflowForecastJob>();
builder.Services.AddScoped<DailyDigestJob>();
builder.Services.AddScoped<TaxReminderJob>();
builder.Services.AddScoped<CashflowAlertJob>();
builder.Services.AddScoped<WeeklyReconciliationJob>();

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

// Greek first: it is the default for a Greek product, and the first entry is
// what an unrecognised or absent cookie falls back to.
var supportedCultures = new[] { Strings.Greek, Strings.English };

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.SetDefaultCulture(Strings.Greek)
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);

    // Cookie only. Honouring Accept-Language would flip a Greek accountant's
    // interface to English just because their browser is, which is not the
    // signal we want — the toggle is explicit.
    options.RequestCultureProviders =
        [new Microsoft.AspNetCore.Localization.CookieRequestCultureProvider()];
});

var app = builder.Build();

// Bring the schema up to date before the first request is served.
//
// Until now every migration needed someone to remember to run `dotnet ef
// database update` against the remote database by hand, and the one time that
// was missed, every authenticated page on staging returned 500 — the table was
// simply not there.
//
// Failure is logged, not thrown. The landing page, sign-in and /health need no
// schema at all, and an instance that still serves those and says loudly what
// is wrong is more useful than one that will not boot.
//
// NOTE: this is right for a single instance (staging, early production). For
// multi-instance zero-downtime deploys, move it to a CI/CD step that runs
// before the new version rolls out — two instances migrating at once is a race
// this does not solve.
// See https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying
await using (var migrationScope = app.Services.CreateAsyncScope())
{
    var logger = migrationScope.ServiceProvider.GetRequiredService<ILoggerFactory>()
        .CreateLogger("Roivo.Startup.Migrations");

    try
    {
        // Through the factory: the scoped context is built per request and the
        // tenant filter it carries is meaningless outside one.
        var factory = migrationScope.ServiceProvider
            .GetRequiredService<IDbContextFactory<ApplicationDbContext>>();

        await using var db = await factory.CreateDbContextAsync();

        var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();

        if (pending.Count == 0)
        {
            logger.LogInformation("Database is up to date, no pending migrations");
        }
        else
        {
            logger.LogInformation(
                "Applying {Count} pending migration(s): {Migrations}",
                pending.Count, string.Join(", ", pending));

            await db.Database.MigrateAsync();

            logger.LogInformation("Applied {Count} migration(s)", pending.Count);
        }
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Could not apply database migrations; starting anyway");
    }
}


// Render terminates TLS and forwards plain HTTP to the container, so without
// this the app sees http:// and OpenIddict rejects its own endpoints as
// insecure. Must run before anything that reads the scheme or client IP.
var forwardedHeaders = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
};

// KnownProxies/KnownIPNetworks default to loopback only, which silently drops the
// headers behind Render's proxy — its address is not fixed, and only the
// platform can reach the container, so trust the hop. Clearing is required:
// an empty collection initializer would leave the loopback defaults in place.
forwardedHeaders.KnownIPNetworks.Clear();
forwardedHeaders.KnownProxies.Clear();

app.UseForwardedHeaders(forwardedHeaders);

// Before anything that renders: the culture has to be set for the request
// before a component reads a resource string.
app.UseRequestLocalization(app.Services.GetRequiredService<
    Microsoft.Extensions.Options.IOptions<RequestLocalizationOptions>>().Value);

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

// 06:30 Athens: after the 06:00 forecast, which it reads. Running it before
// would alert on yesterday's projection.
recurringJobs.AddOrUpdate<CashflowAlertJob>(
    "cashflow-alert",
    job => job.Execute(CancellationToken.None),
    "30 6 * * *",
    new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Athens") });

// 07:00 Athens: the accountant's morning summary, once the night's syncing,
// reconciliation and forecasting have all landed.
recurringJobs.AddOrUpdate<DailyDigestJob>(
    "daily-digest",
    job => job.Execute(CancellationToken.None),
    Cron.Daily(7),
    new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Athens") });

// 08:00 Athens: tax reminders, ahead of the 09:00 failure notifications so a
// deadline lands before the noise.
recurringJobs.AddOrUpdate<TaxReminderJob>(
    "tax-reminder",
    job => job.Execute(CancellationToken.None),
    Cron.Daily(8),
    new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Athens") });

// Monday 07:00 Athens: the week's reconciliation summary.
recurringJobs.AddOrUpdate<WeeklyReconciliationJob>(
    "weekly-reconciliation",
    job => job.Execute(CancellationToken.None),
    Cron.Weekly(DayOfWeek.Monday, 7),
    new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Athens") });

// Keep-alive target for cron-job.org so Render's free tier does not idle the
// instance. Deliberately touches neither the database nor any service, so a
// cold or unhealthy dependency cannot make the ping fail and spin up alerts.
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("Health")
    .AllowAnonymous();

// Schema state, so the deploy can be checked without database access. It names
// migrations — timestamps and descriptions — and never reads a row, so there is
// nothing here to authenticate.
app.MapGet("/health/db", async (IDbContextFactory<ApplicationDbContext> factory) =>
{
    try
    {
        await using var db = await factory.CreateDbContextAsync();

        var applied = (await db.Database.GetAppliedMigrationsAsync()).ToList();
        var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();

        return Results.Ok(new
        {
            status = pending.Count == 0 ? "healthy" : "pending_migrations",
            applied = applied.Count,
            pending = pending.Count,
            pendingMigrations = pending,
            lastApplied = applied.LastOrDefault(),
        });
    }
    catch (Exception ex)
    {
        // 503 rather than 500: the schema is unreachable or unreadable, which is
        // exactly what a health probe should report as unavailable.
        return Results.Json(new { status = "error", message = ex.Message }, statusCode: 503);
    }
})
    .WithName("DatabaseHealth")
    .AllowAnonymous();

app.MapRazorPages();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
