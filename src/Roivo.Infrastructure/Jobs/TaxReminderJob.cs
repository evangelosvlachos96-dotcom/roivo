using Microsoft.Extensions.Logging;
using Roivo.Application.Abstractions;
using Roivo.Application.Features.Notifications;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Enums;
using Roivo.Infrastructure.Email;
using Roivo.Infrastructure.Email.Templates;
using Roivo.Resources;

namespace Roivo.Infrastructure.Jobs;

/// <summary>
/// Recurring Hangfire job. Emails a reminder twice per obligation: once at the
/// recipient's chosen lead time, and once the day before it falls due.
/// </summary>
/// <remarks>
/// The two reminders are picked by exact date arithmetic — an obligation due on
/// <c>today + leadDays</c> or on <c>today + 1</c> — so the job naturally fires
/// on two days out of the whole lead window rather than every morning in
/// between. <see cref="INotificationDispatchLog"/> then guards against a second
/// send on either of those two days from a retry or a manual trigger.
/// </remarks>
public sealed class TaxReminderJob
{
    /// <summary>
    /// Dedupe lookback. Longer than the maximum lead time so a reminder's stamp
    /// is still visible for every day it could be re-evaluated.
    /// </summary>
    private static readonly TimeSpan DedupeWindow =
        TimeSpan.FromDays(NotificationSettings.MaxTaxReminderDaysBefore + 7);

    /// <summary>The last-call reminder, regardless of the configured lead time.</summary>
    private const int FinalReminderDaysBefore = 1;

    private readonly INotificationSettingsRepository _settings;
    private readonly ICashflowRepository _cashflow;
    private readonly INotificationDispatchLog _dispatchLog;
    private readonly NotificationEmailRenderer _renderer;
    private readonly IEmailSender _emailSender;
    private readonly IClock _clock;
    private readonly ILogger<TaxReminderJob> _logger;

    public TaxReminderJob(
        INotificationSettingsRepository settings,
        ICashflowRepository cashflow,
        INotificationDispatchLog dispatchLog,
        NotificationEmailRenderer renderer,
        IEmailSender emailSender,
        IClock clock,
        ILogger<TaxReminderJob> logger)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(cashflow);
        ArgumentNullException.ThrowIfNull(dispatchLog);
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(emailSender);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(logger);

        _settings = settings;
        _cashflow = cashflow;
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
            .ListEnabledAcrossAllTenantsAsync(NotificationKind.TaxReminder, cancellationToken)
            .ConfigureAwait(false);

        if (recipients.Count == 0)
        {
            _logger.LogInformation("Tax reminder: nobody has it enabled");
            return;
        }

        var today = _clock.Today;

        foreach (var recipient in recipients)
        {
            // A lead time of one collapses the two reminders into the same day;
            // the set keeps that from sending twice.
            var leadTimes = new SortedSet<int> { recipient.TaxReminderDaysBefore, FinalReminderDaysBefore };

            foreach (var daysBefore in leadTimes)
            {
                var dueDate = today.AddDays(daysBefore);

                var obligations = await _cashflow
                    .ListTaxObligationsAsync(recipient.BusinessId, dueDate, dueDate, cancellationToken)
                    .ConfigureAwait(false);

                var unpaid = obligations.Where(o => !o.IsPaid).ToList();
                if (unpaid.Count == 0)
                    continue;

                var dispatchKey = $"tax:{recipient.SettingsId}:{dueDate:yyyy-MM-dd}:{daysBefore}";

                if (await _dispatchLog
                    .WasSentAsync(recipient.TenantId, dispatchKey, DedupeWindow, cancellationToken)
                    .ConfigureAwait(false))
                {
                    _logger.LogDebug(
                        "Tax reminder {Key} already sent for business {BusinessId}",
                        dispatchKey, recipient.BusinessId);
                    continue;
                }

                var model = new TaxReminderEmail.Model(
                    recipient.BusinessId,
                    recipient.BusinessName,
                    daysBefore,
                    [.. unpaid.Select(o => new TaxReminderEmail.Row(
                        Label(o.TaxType), o.DueDate, o.ExpectedAmount))]);

                var email = _renderer.Render(model);

                // One undeliverable address must not abort the run. Resend throws
                // on any 4xx and Polly deliberately does not retry those, so without
                // this a single rejected recipient would starve everyone ordered
                // after it — on this run and identically on every later one.
                try
                {
                    await _emailSender
                        .SendEmailAsync(recipient.Email, email.Subject, email.HtmlBody, cancellationToken)
                        .ConfigureAwait(false);

                    await _dispatchLog
                        .RecordSentAsync(recipient.TenantId, dispatchKey, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to send the tax reminder to {Recipient} for business {BusinessId}; continuing with the rest",
                        recipient.Email, recipient.BusinessId);
                    continue;
                }

                _logger.LogInformation(
                    "Sent tax reminder to {Recipient} for business {BusinessId}: {Count} obligations due {DueDate}",
                    recipient.Email, recipient.BusinessId, unpaid.Count, dueDate);
            }
        }
    }

    private static string Label(TaxType taxType) => taxType switch
    {
        TaxType.Vat => Cashflow.Tax_Vat,
        TaxType.IncomeTax => Cashflow.Tax_IncomeTax,
        TaxType.WithholdingTax => Cashflow.Tax_WithholdingTax,
        TaxType.SocialSecurity => Cashflow.Tax_SocialSecurity,
        TaxType.ProfessionalTax => Cashflow.Tax_ProfessionalTax,
        TaxType.TaxPrepayment => Cashflow.Tax_TaxPrepayment,
        TaxType.PropertyTax => Notifications.TaxLabel_PropertyTax,
        _ => taxType.ToString(),
    };
}
