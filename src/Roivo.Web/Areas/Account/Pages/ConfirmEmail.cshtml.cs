using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Roivo.Core.Domain.Entities;
using Roivo.Infrastructure.Auditing;

namespace Roivo.Web.Areas.Account.Pages;

public class ConfirmEmailModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _audit;

    public ConfirmEmailModel(UserManager<ApplicationUser> userManager, IAuditService audit)
    {
        _userManager = userManager;
        _audit = audit;
    }

    public bool Succeeded { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? userId, string? token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
        {
            Succeeded = false;
            return Page();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            Succeeded = false;
            return Page();
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);
        Succeeded = result.Succeeded;

        if (Succeeded)
        {
            await _audit.LogAsync(
                action: "EmailConfirmed",
                userId: user.Id,
                tenantId: user.TenantId,
                entityType: nameof(ApplicationUser),
                entityId: user.Id.ToString(),
                cancellationToken: cancellationToken);

            TempData["LoginMessage"] = "Το email σου επιβεβαιώθηκε. Συνδέσου τώρα.";
        }

        return Page();
    }
}
