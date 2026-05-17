using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;

namespace Roivo.Web.Areas.Account.Pages;

public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditWriter _audit;

    public LoginModel(
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

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? SuccessMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Το email είναι υποχρεωτικό")]
        [EmailAddress(ErrorMessage = "Μη έγκυρο email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ο κωδικός είναι υποχρεωτικός")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }

    public void OnGet()
    {
        SuccessMessage = TempData["LoginMessage"] as string;
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(
            Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user is not null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                await _audit.WriteAsync(
                    action: "LoginSucceeded",
                    userId: user.Id,
                    tenantId: user.TenantId,
                    entityType: nameof(ApplicationUser),
                    entityId: user.Id.ToString(),
                    cancellationToken: cancellationToken);
            }

            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return LocalRedirect(ReturnUrl);
            }
            return Redirect("/dashboard");
        }

        if (result.RequiresTwoFactor)
        {
            ModelState.AddModelError(string.Empty, "Λάθος email ή κωδικός");
            return Page();
        }

        if (result.IsLockedOut)
        {
            var lockedUser = await _userManager.FindByEmailAsync(Input.Email);
            await _audit.WriteAsync(
                action: "AccountLockedOut",
                userId: lockedUser?.Id,
                tenantId: lockedUser?.TenantId,
                entityType: nameof(ApplicationUser),
                entityId: lockedUser?.Id.ToString(),
                details: $"email={Input.Email}",
                cancellationToken: cancellationToken);
            ModelState.AddModelError(string.Empty, "Ο λογαριασμός σου είναι κλειδωμένος για 15 λεπτά");
            return Page();
        }

        if (result.IsNotAllowed)
        {
            var blockedUser = await _userManager.FindByEmailAsync(Input.Email);
            await _audit.WriteAsync(
                action: "LoginBlockedNotAllowed",
                userId: blockedUser?.Id,
                tenantId: blockedUser?.TenantId,
                entityType: nameof(ApplicationUser),
                entityId: blockedUser?.Id.ToString(),
                details: $"email={Input.Email}",
                cancellationToken: cancellationToken);
            ModelState.AddModelError(string.Empty, "Επιβεβαίωσε το email σου πρώτα");
            return Page();
        }

        await _audit.WriteAsync(
            action: "LoginFailed",
            details: $"email={Input.Email}",
            cancellationToken: cancellationToken);
        ModelState.AddModelError(string.Empty, "Λάθος email ή κωδικός");
        return Page();
    }
}
