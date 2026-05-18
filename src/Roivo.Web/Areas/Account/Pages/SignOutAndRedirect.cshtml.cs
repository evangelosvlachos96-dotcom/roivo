using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Roivo.Core.Domain.Entities;

namespace Roivo.Web.Areas.Account.Pages;

public class SignOutAndRedirectModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;

    public SignOutAndRedirectModel(SignInManager<ApplicationUser> signInManager)
    {
        ArgumentNullException.ThrowIfNull(signInManager);
        _signInManager = signInManager;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        // Use Identity's SignInManager.SignOutAsync — it knows which scheme
        // Identity is using internally and signs out of the correct one.
        await _signInManager.SignOutAsync();
        return Redirect("/");
    }
}