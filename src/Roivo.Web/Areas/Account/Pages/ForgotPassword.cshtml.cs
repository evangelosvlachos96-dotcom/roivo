using System.ComponentModel.DataAnnotations;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Auditing;
using Roivo.Core.Domain.Entities;
using Roivo.Infrastructure.Email;
using Roivo.Infrastructure.Email.Templates;
using Roivo.Resources;

namespace Roivo.Web.Areas.Account.Pages;

public class ForgotPasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IBackgroundJobClient _backgroundJobs;
    private readonly IAuditWriter _audit;
    private readonly ILogger<ForgotPasswordModel> _logger;

    public ForgotPasswordModel(
        UserManager<ApplicationUser> userManager,
        IBackgroundJobClient backgroundJobs,
        IAuditWriter audit,
        ILogger<ForgotPasswordModel> logger)
    {
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(backgroundJobs);
        ArgumentNullException.ThrowIfNull(audit);
        ArgumentNullException.ThrowIfNull(logger);

        _userManager = userManager;
        _backgroundJobs = backgroundJobs;
        _audit = audit;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool Submitted { get; set; }

    public class InputModel
    {
        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.EmailRequired))]
        [EmailAddress(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.EmailInvalid))]
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

            // Rendered on the request thread so the template follows the
            // visitor's language; the Hangfire worker that sends it has none.
            var reset = PasswordResetEmail.Render(
                new PasswordResetEmail.Model(resetUrl),
                baseUrl: $"{Request.Scheme}://{Request.Host}",
                english: Strings.IsEnglish);

            // Enqueue the email send so the user gets the "check your inbox"
            // page immediately. Hangfire will retry on SMTP failure.
            _backgroundJobs.Enqueue<EmailJob>(job =>
                job.SendAsync(Input.Email, reset.Subject, reset.HtmlBody));

            await _audit.WriteAsync(
                action: AuditAction.PasswordResetRequested,
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
