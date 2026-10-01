using System.Security.Claims;
using Hangfire.Dashboard;
using Microsoft.Extensions.Options;
using Roivo.Web.Configuration.Settings;

namespace Roivo.Web.Hangfire;

/// <summary>
/// Restricts the Hangfire dashboard to signed-in users on the configured
/// allowlist. Denies everyone when the allowlist is empty.
/// </summary>
/// <remarks>
/// The dashboard can retry and delete jobs and shows job arguments and stack
/// traces for every tenant, so "authenticated" alone is not a sufficient bar in
/// a multi-tenant app. See <see cref="HangfireDashboardSettings"/>.
/// </remarks>
public sealed class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var httpContext = context.GetHttpContext();
        var user = httpContext.User;

        if (user?.Identity?.IsAuthenticated != true)
            return false;

        // Resolved per request: the allowlist can change without a redeploy.
        var settings = httpContext.RequestServices
            .GetRequiredService<IOptionsSnapshot<HangfireDashboardSettings>>().Value;

        var allowed = settings.ParseAuthorizedEmails();
        if (allowed.Count == 0)
            return false;

        // Identity stores the email under ClaimTypes.Email; fall back to the
        // name claim, which is the email when sign-in uses it as the username.
        var email = user.FindFirst(ClaimTypes.Email)?.Value
            ?? user.FindFirst(ClaimTypes.Name)?.Value;

        return email is not null && allowed.Contains(email);
    }
}
