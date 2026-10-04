using Microsoft.Extensions.Logging;
using Roivo.Application.Abstractions;
using Roivo.Application.Features.Notifications;
using Roivo.Core.Domain.Enums;
using Roivo.Infrastructure.Email;
using Roivo.Infrastructure.Email.Templates;

namespace Roivo.Infrastructure.Jobs;

/// <summary>
/// Recurring Hangfire job. Emails a weekly reconciliation scorecard: how much of
/// the week matched itself and what is still waiting on a human.
/// </summary>
/// <remarks>
/// The window is the seven days ending yesterday, and the dispatch key is that
/// window's start date, so the summary is sent once per week no matter how often
/// the job is triggered — there is no weekday guard, which keeps a manual run
/// useful without risking a second copy.
/// </remarks>
public sealed class WeeklyReconciliationJob
{
    /// <summary>Length of the reported window.</summary>
    public const int WindowDays = 7;

    /// <summary>
    /// Dedupe lookback, two windows wide so last week's stamp is still visible
    /// while this week's is being decided.
    /// </summary>
    private static readonly TimeSpan DedupeWindow = TimeSpan.FromDays(2 * WindowDays);

    private readonly INotificationSettingsRepository _settings;
    private readonly IReconciliationRepository _reconciliation;
    private readonly INotificationDispatchLog _dispatchLog;
    private readonly NotificationEmailRenderer _renderer;
    private readonly IEmailSender _emailSender;
    private readonly IClock _clock;
    private readonly ILogger<WeeklyReconciliationJob> _logger;

    public WeeklyReconciliationJob(
        INotificationSettingsRepository settings,
        IReconciliationRepository reconciliation,
        INotificationDispatchLog dispatchLog,
        NotificationEmailRenderer renderer,
        IEmailSender emailSender,
        IClock clock,
        ILogger<WeeklyReconciliationJob> logger)
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
            .ListEnabledAcrossAllTenantsAsync(NotificationKind.WeeklyReconciliation, cancellationToken)
            .ConfigureAwait(false);

        if (recipients.Count == 0)
        {
            _logger.LogInformation("Weekly reconciliation summary: nobody has it enabled");
            return;
        }

        var to = _clock.Today.AddDays(-1);
        var from = to.AddDays(-(WindowDays - 1));

        foreach (var recipient in recipients)
        {
            var dispatchKey = $"recon:{recipient.SettingsId}:{from:yyyy-MM-dd}";

            if (await _dispatchLog
                .WasSentAsync(recipient.TenantId, dispatchKey, DedupeWindow, cancellationToken)
                .ConfigureAwait(false))
            {
                _logger.LogDebug(
                    "Weekly reconciliation summary for business {BusinessId} week of {From} already sent",
                    recipient.BusinessId, from);
                continue;
            }

            var counts = await _reconciliation
                .GetCountsAsync(recipient.BusinessId, from, to, cancellationToken)
                .ConfigureAwait(false);

            // A business with no invoices and no transactions in the window had
            // no week to report on.
            if (counts is { TotalInvoices: 0, TotalTransactions: 0 })
            {
                _logger.LogDebug(
                    "Weekly reconciliation summary for business {BusinessId}: no activity, skipping",
                    recipient.BusinessId);
                continue;
            }

            var email = _renderer.Render(new WeeklyReconciliationEmail.Model(
                recipient.BusinessId,
                recipient.BusinessName,
                from,
                to,
                MatchRate(counts),
                counts.ConfirmedMatches,
                counts.PendingMatches,
                counts.UnreconciledInvoices,
                counts.UnreconciledTransactions));

            await _emailSender
                .SendEmailAsync(recipient.Email, email.Subject, email.HtmlBody, cancellationToken)
                .ConfigureAwait(false);

            await _dispatchLog
                .RecordSentAsync(recipient.TenantId, dispatchKey, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Sent weekly reconciliation summary to {Recipient} for business {BusinessId} ({From} to {To})",
                recipient.Email, recipient.BusinessId, from, to);
        }
    }

    /// <summary>
    /// Share of the window's invoices that are accounted for. Measured on
    /// invoices rather than on matches because an invoice is what the user is
    /// waiting to clear.
    /// </summary>
    private static decimal MatchRate(ReconciliationCounts counts)
        => counts.TotalInvoices == 0
            ? 0m
            : (counts.TotalInvoices - counts.UnreconciledInvoices) / (decimal)counts.TotalInvoices;
}
