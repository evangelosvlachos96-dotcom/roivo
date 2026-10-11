using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Roivo.Core.Domain.Entities;

namespace Roivo.Web.Areas.Account.Pages;

/// <summary>
/// Re-issues the auth cookie against the user's current security stamp, then
/// returns them where they were.
/// </summary>
/// <remarks>
/// Changing a password rotates the security stamp, which leaves the cookie the
/// user is holding stale — Identity's validator would sign them out within the
/// half-hour. Refreshing the cookie is the fix, and it can only happen on a
/// real request: a Blazor circuit has no response left to write cookies to. So
/// Settings changes the password and then sends the browser through here.
/// </remarks>
public class RefreshSignInModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public RefreshSignInModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        ArgumentNullException.ThrowIfNull(signInManager);
        ArgumentNullException.ThrowIfNull(userManager);

        _signInManager = signInManager;
        _userManager = userManager;
    }

    public async Task<IActionResult> OnGetAsync(string? redirectUri)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user is not null)
            await _signInManager.RefreshSignInAsync(user);

        // Only ever redirect inside this site — an attacker-supplied absolute
        // URL here would turn the refresh into an open redirect.
        return LocalRedirect(
            !string.IsNullOrWhiteSpace(redirectUri) && Url.IsLocalUrl(redirectUri)
                ? redirectUri
                : "/dashboard");
    }
}
