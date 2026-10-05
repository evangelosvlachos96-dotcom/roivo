using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Roivo.Resources;

namespace Roivo.Web.Areas.Account.Pages;

/// <summary>
/// Writes the culture cookie and returns the user where they were.
/// </summary>
/// <remarks>
/// A plain GET endpoint rather than a Blazor interaction: the culture is read by
/// middleware on the next request, so the switch only takes effect after a full
/// reload. Doing it here makes that reload explicit instead of leaving a circuit
/// rendering half-translated.
/// </remarks>
[AllowAnonymous]
public class SetLanguageModel : PageModel
{
    public IActionResult OnGet(string? culture, string? redirectUri)
    {
        var requested = culture?.Trim().ToLowerInvariant() switch
        {
            Strings.English => Strings.English,
            _ => Strings.Greek,
        };

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(requested)),
            new CookieOptions
            {
                // A year: the choice is a preference, not a session detail.
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
            });

        // Only ever redirect inside this site — an attacker-supplied absolute
        // URL here would turn the language switch into an open redirect.
        return LocalRedirect(
            !string.IsNullOrWhiteSpace(redirectUri) && Url.IsLocalUrl(redirectUri)
                ? redirectUri
                : "/");
    }
}
