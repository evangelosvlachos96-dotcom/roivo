using MudBlazor.Services;
using Roivo.Web.Components;
using Roivo.Web.Configuration;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseRoivoLogging();

builder.Services
    .AddRoivoPersistence(builder.Configuration)
    .AddRoivoIdentity(builder.Configuration)
    .AddRoivoOpenIddict(builder.Configuration)
    .AddRoivoSecurity(builder.Configuration);

builder.Services.AddMudServices();
builder.Services.AddRazorPages();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

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
app.MapRazorPages();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
