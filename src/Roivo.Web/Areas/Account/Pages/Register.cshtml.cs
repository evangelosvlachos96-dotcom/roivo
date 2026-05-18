using System.ComponentModel.DataAnnotations;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Enums;
using Roivo.Core.Domain.Validation;
using Roivo.Infrastructure.Email;
using Roivo.Infrastructure.Persistence;

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
        [Required(ErrorMessage = "Επίλεξε τύπο λογαριασμού")]
        public TenantType Type { get; set; }

        [Required(ErrorMessage = "Το ονοματεπώνυμο είναι υποχρεωτικό")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Το ονοματεπώνυμο πρέπει να έχει τουλάχιστον 2 χαρακτήρες")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Το email είναι υποχρεωτικό")]
        [EmailAddress(ErrorMessage = "Μη έγκυρο email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ο κωδικός είναι υποχρεωτικός")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Επιβεβαίωσε τον κωδικό")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Οι κωδικοί δεν ταιριάζουν")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Η επωνυμία είναι υποχρεωτική")]
        [StringLength(200)]
        public string OrganizationName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ο ΑΦΜ είναι υποχρεωτικός")]
        [RegularExpression("^[0-9]{9}$", ErrorMessage = "Ο ΑΦΜ πρέπει να είναι 9 ψηφία")]
        [ValidAfm]
        public string Afm { get; set; } = string.Empty;

        [Range(typeof(bool), "true", "true", ErrorMessage = "Πρέπει να αποδεχτείς τους όρους")]
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
                action: "RegistrationAfmDuplicateWarningShown",
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

        var html = $$"""
            <div style="font-family:Arial,Helvetica,sans-serif;max-width:560px;margin:0 auto;padding:24px;color:#222;">
              <h2 style="color:#1565c0;">Καλωσήρθες στο Roivo</h2>
              <p>Γεια σου {{System.Net.WebUtility.HtmlEncode(Input.FullName)}},</p>
              <p>Πάτησε το παρακάτω κουμπί για να επιβεβαιώσεις το email σου και να ενεργοποιήσεις τον λογαριασμό σου.</p>
              <p style="text-align:center;margin:32px 0;">
                <a href="{{encodedToken}}" style="background:#1565c0;color:#fff;padding:12px 28px;border-radius:6px;text-decoration:none;font-weight:600;">Επιβεβαίωση email</a>
              </p>
              <p style="font-size:13px;color:#666;">Αν το κουμπί δεν λειτουργεί, αντίγραψε αυτό το link στον browser σου:</p>
              <p style="font-size:12px;color:#666;word-break:break-all;">{{encodedToken}}</p>
              <hr style="border:none;border-top:1px solid #eee;margin:24px 0;" />
              <p style="font-size:12px;color:#888;">Αν δεν εγγράφηκες στο Roivo, μπορείς να αγνοήσεις αυτό το μήνυμα.</p>
            </div>
            """;

        // Enqueue the email send. Hangfire runs it in the background and retries
        // on failure; the user gets the confirmation-pending page immediately
        // without waiting for SMTP.
        _backgroundJobs.Enqueue<EmailJob>(job => job.SendAsync(Input.Email, "Επιβεβαίωση email - Roivo", html));

        await _audit.WriteAsync(
            action: "UserRegistered",
            userId: user.Id,
            tenantId: tenant.Id,
            entityType: nameof(ApplicationUser),
            entityId: user.Id.ToString(),
            details: new { Type = Input.Type.ToString() },
            cancellationToken: cancellationToken);

        return RedirectToPage("/RegisterConfirmation", new { email = Input.Email });
    }
}
