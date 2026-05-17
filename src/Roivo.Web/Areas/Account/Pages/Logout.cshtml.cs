using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;

namespace Roivo.Web.Areas.Account.Pages;

public class LogoutModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditWriter _audit;

    public LogoutModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuditWriter audit)
    {
        ArgumentNullException.ThrowIfNull(signInManager);
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(audit);

        _signInManager = signInManager;
        _userManager = userManager;
        _audit = audit;
    }

    public IActionResult OnGet() => Redirect("/");

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        await _signInManager.SignOutAsync();

        await _audit.WriteAsync(
            action: "LoggedOut",
            userId: user?.Id,
            tenantId: user?.TenantId,
            entityType: nameof(ApplicationUser),
            entityId: user?.Id.ToString(),
            cancellationToken: cancellationToken);

        return Redirect("/");
    }
}
