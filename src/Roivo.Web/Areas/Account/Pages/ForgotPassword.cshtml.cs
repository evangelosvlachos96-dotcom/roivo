using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;
using Roivo.Infrastructure.Email;

namespace Roivo.Web.Areas.Account.Pages;

public class ForgotPasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly IAuditWriter _audit;
    private readonly ILogger<ForgotPasswordModel> _logger;

    public ForgotPasswordModel(
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender,
        IAuditWriter audit,
        ILogger<ForgotPasswordModel> logger)
    {
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(emailSender);
        ArgumentNullException.ThrowIfNull(audit);
        ArgumentNullException.ThrowIfNull(logger);

        _userManager = userManager;
        _emailSender = emailSender;
        _audit = audit;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool Submitted { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Το email είναι υποχρεωτικό")]
        [EmailAddress(ErrorMessage = "Μη έγκυρο email")]
        public string Email { get; set; } = string.Empty;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user is not null && await _userManager.IsEmailConfirmedAsync(user))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetUrl = QueryHelpers.AddQueryString(
                $"{Request.Scheme}://{Request.Host}/Account/ResetPassword",
                new Dictionary<string, string?>
                {
                    ["email"] = Input.Email,
                    ["token"] = token,
                });

            var html = $$"""
                <div style="font-family:Arial,Helvetica,sans-serif;max-width:560px;margin:0 auto;padding:24px;color:#222;">
                  <h2 style="color:#1565c0;">Επαναφορά κωδικού</h2>
                  <p>Λάβαμε αίτημα για επαναφορά του κωδικού σου. Πάτησε το παρακάτω κουμπί για να ορίσεις νέο κωδικό.</p>
                  <p style="text-align:center;margin:32px 0;">
                    <a href="{{resetUrl}}" style="background:#1565c0;color:#fff;padding:12px 28px;border-radius:6px;text-decoration:none;font-weight:600;">Επαναφορά κωδικού</a>
                  </p>
                  <p style="font-size:13px;color:#666;">Αν το κουμπί δεν λειτουργεί, αντίγραψε αυτό το link στον browser σου:</p>
                  <p style="font-size:12px;color:#666;word-break:break-all;">{{resetUrl}}</p>
                  <hr style="border:none;border-top:1px solid #eee;margin:24px 0;" />
                  <p style="font-size:12px;color:#888;">Αν δεν ζήτησες επαναφορά κωδικού, αγνόησε αυτό το μήνυμα.</p>
                </div>
                """;

            try
            {
                await _emailSender.SendEmailAsync(Input.Email, "Επαναφορά κωδικού - Roivo", html, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", Input.Email);
            }

            await _audit.WriteAsync(
                action: "PasswordResetRequested",
                userId: user.Id,
                tenantId: user.TenantId,
                entityType: nameof(ApplicationUser),
                entityId: user.Id.ToString(),
                cancellationToken: cancellationToken);
        }

        Submitted = true;
        return Page();
    }
}
