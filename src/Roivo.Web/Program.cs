using Hangfire;
using MudBlazor.Services;
using Roivo.Aade.Configuration;
using Roivo.Aade.Jobs;
using Roivo.Application.Configuration;
using Roivo.Web.Components;
using Roivo.Web.Configuration;
using Roivo.Infrastructure.Jobs;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseRoivoLogging();

builder.Services
    .AddRoivoPersistence(builder.Configuration)
    .AddRoivoApplication()
    .AddRoivoBackgroundJobs(builder.Configuration)
    .AddRoivoAade(builder.Configuration)
    .AddRoivoIdentity(builder.Configuration)
    .AddRoivoOpenIddict(builder.Configuration)
    .AddRoivoSecurity(builder.Configuration);

builder.Services.AddMudServices();
builder.Services.AddRazorPages();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Hangfire activates background jobs from the DI container; register here so
// the recurring registration below can resolve the type.
builder.Services.AddScoped<AadeFailureNotificationJob>();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseSecurityHeaders();
app.UseRoivoErrorHandling();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

if (app.Environment.IsDevelopment())
{
    // Dashboard is unauthenticated by default; only mount it in dev where
    // anything that could reach it is already a trusted developer.
    app.UseHangfireDashboard("/hangfire");
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

app.MapRazorPages();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
