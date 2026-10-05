using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;
using Roivo.Infrastructure.Email;
using Roivo.Infrastructure.Email.Templates;
using Roivo.Resources;

namespace Roivo.Infrastructure.Jobs;

/// <summary>
/// Recurring Hangfire job. Finds every active business whose banking failure
/// streak has exceeded 24 hours and that has not yet been emailed, notifies the
/// owner, and stamps the email-sent time so the message isn't repeated. Mirrors
/// <see cref="AadeFailureNotificationJob"/>.
/// </summary>
public sealed class BankingFailureNotificationJob
{
    // Matches the AADE window and the resilience-patterns doc: transient blips
    // shouldn't bother the user; only a sustained failure warrants email.
    private static readonly TimeSpan FailureNotificationWindow = TimeSpan.FromHours(24);

    private readonly IBusinessRepository _businesses;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BankingFailureNotificationJob> _logger;

    public BankingFailureNotificationJob(
        IBusinessRepository businesses,
        IEmailSender emailSender,
        IConfiguration configuration,
        ILogger<BankingFailureNotificationJob> logger)
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
        var targets = await _businesses.ListWithExpiredBankingFailureAsync(threshold, cancellationToken).ConfigureAwait(false);

        if (targets.Count == 0)
        {
            _logger.LogInformation("Banking failure notification: no businesses past the {Hours}h threshold", FailureNotificationWindow.TotalHours);
            return;
        }

        var baseUrl = (_configuration["App:PublicBaseUrl"] ?? "https://roivo.gr").TrimEnd('/');

        foreach (var business in targets)
        {
            var ownerEmail = await _businesses.GetTenantPrimaryEmailAsync(business.TenantId, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(ownerEmail))
            {
                _logger.LogWarning(
                    "Skipping banking failure notification for business {BusinessId}: no confirmed owner email on tenant {TenantId}",
                    business.Id, business.TenantId);
                continue;
            }

            var email = RenderEmail(business, baseUrl);
            await _emailSender.SendEmailAsync(ownerEmail, email.Subject, email.HtmlBody, cancellationToken).ConfigureAwait(false);

            business.RecordBankingFailureEmailSent();
            await _businesses.UpdateAsync(business, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Sent banking failure notification to {Email} for business {BusinessId}",
                ownerEmail, business.Id);
        }
    }

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
                ConnectionLabel: Roivo.Resources.Banking.ConnectionLabel,
                ReasonText: MapFailureReason(business.BankingLastFailureReason),
                // ! is safe: ListWithExpiredBankingFailureAsync filters out null failure timestamps.
                SinceUtc: business.BankingFirstFailureAt!.Value,
                CheckPath: "banking",
                FailedAttempts: business.BankingSyncErrorCount),
            baseUrl,
            english: false);

    private static string MapFailureReason(string? reason) => reason switch
    {
        "SessionExpired" => Roivo.Resources.Banking.FailureReasonSessionExpired,
        "Unauthorized" => Roivo.Resources.Banking.FailureReasonUnauthorized,
        "NetworkError" => Roivo.Resources.Banking.FailureReasonNetwork,
        _ => Roivo.Resources.Banking.FailureReasonGeneric
    };
}
