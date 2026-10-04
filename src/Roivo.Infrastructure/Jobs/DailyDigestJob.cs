using Microsoft.Extensions.Logging;
using Roivo.Application.Abstractions;
using Roivo.Application.Features.Notifications;
using Roivo.Core.Domain.Enums;
using Roivo.Infrastructure.Email;
using Roivo.Infrastructure.Email.Templates;

namespace Roivo.Infrastructure.Jobs;

/// <summary>
/// Recurring Hangfire job. Emails everyone who has the daily digest switched on
/// a one-line-per-business summary of yesterday's activity.
/// </summary>
/// <remarks>
/// Grouped by user rather than by business: an accountant with fifty clients
/// wants one email with fifty lines, not fifty emails. Idempotent for the day
/// through <see cref="INotificationDispatchLog"/>, so a manual re-trigger or a
/// Hangfire retry does not mail the same digest twice.
/// </remarks>
public sealed class DailyDigestJob
{
    /// <summary>
    /// Dedupe lookback. Two days rather than one so a run that slips past
    /// midnight still sees yesterday's stamp.
    /// </summary>
    private static readonly TimeSpan DedupeWindow = TimeSpan.FromDays(2);

    private readonly INotificationSettingsRepository _settings;
    private readonly IReconciliationRepository _reconciliation;
    private readonly INotificationDispatchLog _dispatchLog;
    private readonly NotificationEmailRenderer _renderer;
    private readonly IEmailSender _emailSender;
    private readonly IClock _clock;
    private readonly ILogger<DailyDigestJob> _logger;

    public DailyDigestJob(
        INotificationSettingsRepository settings,
        IReconciliationRepository reconciliation,
        INotificationDispatchLog dispatchLog,
        NotificationEmailRenderer renderer,
        IEmailSender emailSender,
        IClock clock,
        ILogger<DailyDigestJob> logger)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(reconciliation);
        ArgumentNullException.ThrowIfNull(dispatchLog);
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(emailSender);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(logger);

        _settings = settings;
        _reconciliation = reconciliation;
        _dispatchLog = dispatchLog;
        _renderer = renderer;
        _emailSender = emailSender;
        _clock = clock;
        _logger = logger;
    }

    // Hangfire requires a public entry point and calls it with no arguments.
    public async Task Execute(CancellationToken cancellationToken = default)
    {
        var recipients = await _settings
            .ListEnabledAcrossAllTenantsAsync(NotificationKind.DailyDigest, cancellationToken)
            .ConfigureAwait(false);

        if (recipients.Count == 0)
        {
            _logger.LogInformation("Daily digest: nobody has it enabled");
            return;
        }

        // Yesterday, not today: the digest goes out in the morning and reports a
        // closed day. Today's figures would be a few hours old and change again.
        var day = _clock.Today.AddDays(-1);

        foreach (var group in recipients.GroupBy(r => r.UserId, StringComparer.Ordinal))
        {
            var first = group.First();
            var dispatchKey = $"digest:{group.Key}:{day:yyyy-MM-dd}";

            if (await _dispatchLog
                .WasSentAsync(first.TenantId, dispatchKey, DedupeWindow, cancellationToken)
                .ConfigureAwait(false))
            {
                _logger.LogDebug("Daily digest for {UserId} on {Day} already sent", group.Key, day);
                continue;
            }

            var rows = new List<DailyDigestEmail.Row>();
            foreach (var recipient in group)
            {
                var counts = await _reconciliation
                    .GetCountsAsync(recipient.BusinessId, day, day, cancellationToken)
                    .ConfigureAwait(false);

                rows.Add(new DailyDigestEmail.Row(
                    recipient.BusinessId,
                    recipient.BusinessName,
                    counts.TotalInvoices,
                    counts.TotalTransactions,
                    counts.UnreconciledInvoices + counts.UnreconciledTransactions));
            }

            // A digest of nothing is noise. Skipping without stamping means the
            // next run re-checks cheaply rather than closing the day out on a
            // non-send.
            if (rows.All(r => r is { NewInvoices: 0, NewTransactions: 0, Unmatched: 0 }))
            {
                _logger.LogDebug("Daily digest for {UserId}: no activity on {Day}, skipping", group.Key, day);
                continue;
            }

            var email = _renderer.Render(new DailyDigestEmail.Model(day, rows));

            // One undeliverable address must not abort the run. Resend throws
            // on any 4xx and Polly deliberately does not retry those, so without
            // this a single rejected recipient would starve everyone ordered
            // after it — on this run and identically on every later one.
            try
            {
                await _emailSender
                    .SendEmailAsync(first.Email, email.Subject, email.HtmlBody, cancellationToken)
                    .ConfigureAwait(false);

                await _dispatchLog
                    .RecordSentAsync(first.TenantId, dispatchKey, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send the daily digest to {Recipient} for user {UserId}; continuing with the rest",
                    first.Email, group.Key);
                continue;
            }

            _logger.LogInformation(
                "Sent daily digest for {Day} to {Recipient} covering {Count} businesses",
                day, first.Email, rows.Count);
        }
    }
}
