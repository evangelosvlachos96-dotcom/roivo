using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Roivo.Core.Domain.Entities;
using Roivo.Infrastructure.Auditing;

namespace Roivo.Web.Areas.Account.Pages;

public class ResetPasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _audit;

    public ResetPasswordModel(UserManager<ApplicationUser> userManager, IAuditService audit)
    {
        _userManager = userManager;
        _audit = audit;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool Succeeded { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ο κωδικός είναι υποχρεωτικός")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Επιβεβαίωσε τον κωδικό")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Οι κωδικοί δεν ταιριάζουν")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public IActionResult OnGet(string? email, string? token)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
        {
            ModelState.AddModelError(string.Empty, "Το link είναι άκυρο.");
            return Page();
        }

        Input.Email = email;
        Input.Token = token;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user is null)
        {
            Succeeded = true;
            return Page();
        }

        var result = await _userManager.ResetPasswordAsync(user, Input.Token, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return Page();
        }

        await _userManager.UpdateSecurityStampAsync(user);

        await _audit.LogAsync(
            action: "PasswordResetCompleted",
            userId: user.Id,
            tenantId: user.TenantId,
            entityType: nameof(ApplicationUser),
            entityId: user.Id.ToString(),
            cancellationToken: cancellationToken);

        Succeeded = true;
        return Page();
    }
}
