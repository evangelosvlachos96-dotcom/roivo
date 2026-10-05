using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;
using Roivo.Infrastructure.Email;
using Roivo.Infrastructure.Email.Templates;
using Roivo.Resources;

namespace Roivo.Infrastructure.Jobs;

/// <summary>
/// Recurring Hangfire job. Once per run, finds every active business whose
/// AADE failure period has exceeded 24 hours and that has not yet been
/// emailed, sends the owner a notification, and stamps the email-sent time
/// on the entity so the message isn't repeated. The 24h threshold is checked
/// at the database level so single iterations stay cheap.
/// </summary>
public sealed class AadeFailureNotificationJob
{
    // The window matches the resilience-patterns doc: transient blips
    // shouldn't bother the user; only a sustained failure warrants email.
    private static readonly TimeSpan FailureNotificationWindow = TimeSpan.FromHours(24);

    private readonly IBusinessRepository _businesses;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AadeFailureNotificationJob> _logger;

    public AadeFailureNotificationJob(
        IBusinessRepository businesses,
        IEmailSender emailSender,
        IConfiguration configuration,
        ILogger<AadeFailureNotificationJob> logger)
    {
        ArgumentNullException.ThrowIfNull(businesses);
        ArgumentNullException.ThrowIfNull(emailSender);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(logger);

        _businesses = businesses;
        _emailSender = emailSender;
        _configuration = configuration;
        _logger = logger;
    }

    // Hangfire requires a public entry point. Recurring registrations call
    // Execute() with no arguments; the per-business work happens inside.
    public async Task Execute(CancellationToken cancellationToken = default)
    {
        var threshold = DateTime.UtcNow - FailureNotificationWindow;
        var targets = await _businesses.ListWithExpiredAadeFailureAsync(threshold, cancellationToken).ConfigureAwait(false);

        if (targets.Count == 0)
        {
            _logger.LogInformation("AADE failure notification: no businesses past the {Hours}h threshold", FailureNotificationWindow.TotalHours);
            return;
        }

        var baseUrl = (_configuration["App:PublicBaseUrl"] ?? "https://roivo.gr").TrimEnd('/');

        foreach (var business in targets)
        {
            var ownerEmail = await _businesses.GetTenantPrimaryEmailAsync(business.TenantId, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(ownerEmail))
            {
                _logger.LogWarning(
                    "Skipping AADE failure notification for business {BusinessId}: no confirmed owner email on tenant {TenantId}",
                    business.Id, business.TenantId);
                continue;
            }

            var email = RenderEmail(business, baseUrl);
            await _emailSender.SendEmailAsync(ownerEmail, email.Subject, email.HtmlBody, cancellationToken).ConfigureAwait(false);

            business.RecordAadeFailureEmailSent();
            await _businesses.UpdateAsync(business, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Sent AADE failure notification to {Email} for business {BusinessId}",
                ownerEmail, business.Id);
        }
    }

    /// <summary>Product name; the same in both languages.</summary>
    private const string AadeConnectionLabel = "AADE myDATA";

    /// <remarks>
    /// Rendered in Greek rather than the owner's language: the recipient's
    /// choice lives in a culture cookie, which a background job cannot see.
    /// Greek is the product default, so an unattended send belongs there.
    /// </remarks>
    private static RenderedEmail RenderEmail(Business business, string baseUrl) =>
        SyncFailureEmail.Render(
            new SyncFailureEmail.Model(
                BusinessId: business.Id,
                BusinessName: business.Name,
                ConnectionLabel: AadeConnectionLabel,
                ReasonText: MapFailureReason(business.AadeLastFailureReason),
                // ! is safe: ListWithExpiredAadeFailureAsync filters out null failure timestamps.
                SinceUtc: business.AadeLastFailureAt!.Value,
                CheckPath: "aade"),
            baseUrl,
            english: false);

    private static string MapFailureReason(string? reason) => reason switch
    {
        "InvalidCredentials" => Aade.FailureReasonInvalidCredentials,
        "AfmMismatch" => Aade.FailureReasonAfmMismatch,
        _ => Aade.FailureReasonGeneric
    };
}
