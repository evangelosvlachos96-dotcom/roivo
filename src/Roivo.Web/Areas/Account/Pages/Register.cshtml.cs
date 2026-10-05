using System.ComponentModel.DataAnnotations;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Auditing;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Enums;
using Roivo.Core.Domain.Validation;
using Roivo.Infrastructure.Email;
using Roivo.Infrastructure.Email.Templates;
using Roivo.Infrastructure.Persistence;
using Roivo.Resources;

namespace Roivo.Web.Areas.Account.Pages;

public class RegisterModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly IBackgroundJobClient _backgroundJobs;
    private readonly IAuditWriter _audit;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        IBackgroundJobClient backgroundJobs,
        IAuditWriter audit,
        ILogger<RegisterModel> logger)
    {
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(backgroundJobs);
        ArgumentNullException.ThrowIfNull(audit);
        ArgumentNullException.ThrowIfNull(logger);

        _userManager = userManager;
        _db = db;
        _backgroundJobs = backgroundJobs;
        _audit = audit;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool ShowConfirmDuplicate { get; private set; }

    public class InputModel
    {
        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.AccountTypeRequired))]
        public TenantType Type { get; set; }

        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.FullNameRequired))]
        [StringLength(200, MinimumLength = 2, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.FullNameTooShort))]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.EmailRequired))]
        [EmailAddress(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.EmailInvalid))]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.PasswordRequired))]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.ConfirmPasswordRequired))]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.PasswordsDoNotMatch))]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.OrganisationNameRequired))]
        [StringLength(200)]
        public string OrganizationName { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.AfmRequired))]
        [RegularExpression("^[0-9]{9}$", ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.AfmLength))]
        [ValidAfm]
        public string Afm { get; set; } = string.Empty;

        [Range(typeof(bool), "true", "true", ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.TermsMustBeAccepted))]
        public bool AcceptTerms { get; set; }

        // Set to true when the user re-submits after seeing the AFM-already-exists
        // warning. Lets legitimate cases (e.g., same person registering both an
        // accountant tenant and a business tenant) proceed without a hard block.
        public bool ConfirmDuplicate { get; set; }
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // AFMs aren't private (they're on every invoice), so surfacing a duplicate
        // is acceptable — and helps users who forgot they already registered. We
        // restrict the match to same Type so an accountant + business owner with
        // the same AFM can still register both legitimately.
        var existingTenant = await _db.Tenants
            .FirstOrDefaultAsync(t => t.Afm == Input.Afm
                                   && t.Type == Input.Type
                                   && t.IsActive,
                cancellationToken);

        if (existingTenant is not null && !Input.ConfirmDuplicate)
        {
            await _audit.WriteAsync(
                action: AuditAction.RegistrationAfmDuplicateWarningShown,
                tenantId: existingTenant.Id,
                entityType: nameof(Tenant),
                entityId: existingTenant.Id.ToString(),
                details: new { AttemptedEmail = Input.Email, Afm = Input.Afm },
                cancellationToken: cancellationToken);

            ModelState.AddModelError(string.Empty,
                "Φαίνεται ότι υπάρχει ήδη λογαριασμός για αυτό το ΑΦΜ. " +
                "Αν είναι δικός σου, κάνε σύνδεση ή επαναφορά κωδικού. " +
                "Αν θέλεις να συνεχίσεις την εγγραφή ούτως ή άλλως, " +
                "επίλεξε το παρακάτω checkbox και υπέβαλε ξανά.");
            ShowConfirmDuplicate = true;
            return Page();
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var tenant = new Tenant
        {
            Name = Input.OrganizationName,
            Afm = Input.Afm,
            Type = Input.Type,
            IsActive = true,
        };

        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(cancellationToken);

        var user = new ApplicationUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            FullName = Input.FullName,
            TenantId = tenant.Id,
            EmailConfirmed = false,
        };

        var createResult = await _userManager.CreateAsync(user, Input.Password);
        if (!createResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            var enumerationMaskAdded = false;
            foreach (var error in createResult.Errors)
            {
                if (error.Code == "DuplicateUserName" || error.Code == "DuplicateEmail")
                {
                    // Anti-enumeration: don't confirm whether the email is registered.
                    // The genuine owner can still recover via password reset.
                    if (!enumerationMaskAdded)
                    {
                        ModelState.AddModelError(string.Empty,
                            "Δεν μπορέσαμε να δημιουργήσουμε τον λογαριασμό. " +
                            "Αν έχεις ήδη λογαριασμό, κάνε σύνδεση ή επαναφορά κωδικού.");
                        enumerationMaskAdded = true;
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return Page();
        }

        if (Input.Type == TenantType.Business)
        {
            // Registration runs anonymously, so the SaveChanges override can't
            // resolve a tenant_id claim — assign explicitly.
            var business = Business.Create(Input.OrganizationName, Input.Afm, kad: null, address: null);
            business.TenantId = tenant.Id;
            _db.Businesses.Add(business);
            await _db.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = QueryHelpers.AddQueryString(
            $"{Request.Scheme}://{Request.Host}/Account/ConfirmEmail",
            new Dictionary<string, string?>
            {
                ["userId"] = user.Id.ToString(),
                ["token"] = token,
            });

        // Rendered while still on the request thread, so the branded template
        // picks up the culture the visitor registered in. The Hangfire worker
        // that sends it has no culture of its own.
        var confirmation = EmailConfirmationEmail.Render(
            new EmailConfirmationEmail.Model(Input.FullName, encodedToken),
            baseUrl: $"{Request.Scheme}://{Request.Host}",
            english: Strings.IsEnglish);

        // Enqueue the email send. Hangfire runs it in the background and retries
        // on failure; the user gets the confirmation-pending page immediately
        // without waiting for SMTP.
        _backgroundJobs.Enqueue<EmailJob>(job =>
            job.SendAsync(Input.Email, confirmation.Subject, confirmation.HtmlBody));

        await _audit.WriteAsync(
            action: AuditAction.UserRegistered,
            userId: user.Id,
            tenantId: tenant.Id,
            entityType: nameof(ApplicationUser),
            entityId: user.Id.ToString(),
            details: new { Type = Input.Type.ToString() },
            cancellationToken: cancellationToken);

        return RedirectToPage("/RegisterConfirmation", new { email = Input.Email });
    }
}
