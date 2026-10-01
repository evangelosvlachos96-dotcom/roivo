using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;
using Roivo.Infrastructure.Email;
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

            var body = BuildEmailBody(business, baseUrl);
            await _emailSender.SendEmailAsync(ownerEmail, Roivo.Resources.Banking.FailureEmailSubject, body, cancellationToken).ConfigureAwait(false);

            business.RecordBankingFailureEmailSent();
            await _businesses.UpdateAsync(business, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Sent banking failure notification to {Email} for business {BusinessId}",
                ownerEmail, business.Id);
        }
    }

    private static string BuildEmailBody(Business business, string baseUrl)
    {
        var reconnectUrl = $"{baseUrl}/businesses/{business.Id}/banking";
        var reasonText = MapFailureReasonToGreek(business.BankingLastFailureReason);
        // ! is safe: ListWithExpiredBankingFailureAsync filters out null failure timestamps.
        var failureLocal = business.BankingFirstFailureAt!.Value.ToLocalTime();

        var body = string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            Roivo.Resources.Banking.FailureEmailBody,
            business.Name,
            business.Afm,
            failureLocal,
            business.BankingSyncErrorCount,
            reasonText,
            reconnectUrl);

        // IEmailSender takes HTML; convert plain-text newlines.
        return body.Replace("\n", "<br/>") + "<br/><br/>" + Roivo.Resources.Banking.FailureEmailSignature;
    }

    private static string MapFailureReasonToGreek(string? reason) => reason switch
    {
        "SessionExpired" => Roivo.Resources.Banking.FailureReasonSessionExpired,
        "Unauthorized" => Roivo.Resources.Banking.FailureReasonUnauthorized,
        "NetworkError" => Roivo.Resources.Banking.FailureReasonNetwork,
        _ => Roivo.Resources.Banking.FailureReasonGeneric
    };
}
