using System.ComponentModel.DataAnnotations;
using Roivo.Resources;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Roivo.Application.Abstractions;
using Roivo.Application.Abstractions.Security;
using Roivo.Infrastructure.Email;
using Roivo.Infrastructure.Email.Templates;
using Hangfire;
using Roivo.Core.Domain.Auditing;
using Roivo.Core.Domain.Entities;

namespace Roivo.Web.Areas.Account.Pages;

public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditWriter _audit;
    private readonly IDeviceTrackingService _devices;
    private readonly ILogger<LoginModel> _logger;
    private readonly IBackgroundJobClient _backgroundJobs;

    public LoginModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuditWriter audit,
        IDeviceTrackingService devices,
        ILogger<LoginModel> logger,
        IBackgroundJobClient backgroundJobs)
    {
        ArgumentNullException.ThrowIfNull(signInManager);
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(audit);
        ArgumentNullException.ThrowIfNull(devices);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(backgroundJobs);

        _signInManager = signInManager;
        _userManager = userManager;
        _audit = audit;
        _devices = devices;
        _logger = logger;
        _backgroundJobs = backgroundJobs;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? SuccessMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.EmailRequired))]
        [EmailAddress(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.EmailInvalid))]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.PasswordRequired))]
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
                    action: AuditAction.LoginSucceeded,
                    userId: user.Id,
                    tenantId: user.TenantId,
                    entityType: nameof(ApplicationUser),
                    entityId: user.Id.ToString(),
                    cancellationToken: cancellationToken);

                await NoticeNewDeviceAsync(user, cancellationToken);
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
                action: AuditAction.AccountLockedOut,
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
                action: AuditAction.LoginBlockedNotAllowed,
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
            action: AuditAction.LoginFailed,
            details: $"email={Input.Email}",
            cancellationToken: cancellationToken);
        ModelState.AddModelError(string.Empty, "Λάθος email ή κωδικός");
        return Page();
    }

    /// <summary>
    /// Flags a sign-in from a device this account has not used before.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Deliberately cannot fail the sign-in. Device tracking is a courtesy
    /// notice; if the lookup or the write throws, the right outcome is that the
    /// user still gets in and we lose one notification, not that a working
    /// password stops working.
    /// </para>
    /// <para>
    /// The banner is handed over in TempData rather than held in the database.
    /// It is a one-shot message for the next page render, and TempData is
    /// exactly that — it also means dismissing it needs no write.
    /// </para>
    /// </remarks>
    private async Task NoticeNewDeviceAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        try
        {
            var userAgent = Request.Headers.UserAgent.ToString();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (await _devices.IsKnownDeviceAsync(user.Id, userAgent, ip, cancellationToken))
                return;

            var device = await _devices.RegisterDeviceAsync(user.Id, userAgent, ip, cancellationToken);

            // The banner is raised by the shell, which asks the tracker itself.
            // All that is left here is telling the owner out of band, in case
            // the sign-in was not theirs and they never see the banner at all.
            await SendNewDeviceEmailAsync(user, device, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Could not check the sign-in device for user {UserId}", user.Id);
        }
    }

    /// <summary>
    /// Emails the account owner about a sign-in from a device we have not seen.
    /// </summary>
    /// <remarks>
    /// Queued through Hangfire rather than sent inline: the provider call can be
    /// slow or down, and nobody should wait on it — or be refused a sign-in by
    /// it — to reach their dashboard.
    /// </remarks>
    private async Task SendNewDeviceEmailAsync(
        ApplicationUser user, UserDeviceView device, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(user.Email)) return;

        var email = NewDeviceEmail.Render(
            new NewDeviceEmail.Model(
                device.DeviceName,
                device.FirstSeenAt,
                string.IsNullOrWhiteSpace(device.IpPrefix) ? "—" : device.IpPrefix),
            baseUrl: $"{Request.Scheme}://{Request.Host}",
            english: Strings.IsEnglish);

        _backgroundJobs.Enqueue<EmailJob>(job =>
            job.SendAsync(user.Email, email.Subject, email.HtmlBody));

        await Task.CompletedTask;
    }
}
