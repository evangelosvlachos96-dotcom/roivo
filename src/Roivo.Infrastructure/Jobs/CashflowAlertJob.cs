using Microsoft.Extensions.Logging;
using Roivo.Application.Abstractions;
using Roivo.Application.Features.Notifications;
using Roivo.Core.Domain.Enums;
using Roivo.Infrastructure.Email;
using Roivo.Infrastructure.Email.Templates;

namespace Roivo.Infrastructure.Jobs;

/// <summary>
/// Recurring Hangfire job. Warns a recipient when the stored forecast for one of
/// their businesses dips to or below their configured threshold.
/// </summary>
/// <remarks>
/// Reads the projection the nightly forecast job already persisted rather than
/// re-running the engine: the figures are identical and the engine is the
/// expensive part. Scheduled after that job for the same reason.
/// </remarks>
public sealed class CashflowAlertJob
{
    /// <summary>Forecast horizon inspected, matching what the nightly job stores.</summary>
    public const int HorizonDays = 90;

    /// <summary>
    /// How long one alert suppresses the next for the same business. A shortfall
    /// thirty days out stays true for thirty mornings; without a cooldown the
    /// recipient would be told so thirty times and would stop reading.
    /// </summary>
    private static readonly TimeSpan AlertCooldown = TimeSpan.FromDays(7);

    private readonly INotificationSettingsRepository _settings;
    private readonly ICashflowRepository _cashflow;
    private readonly INotificationDispatchLog _dispatchLog;
    private readonly NotificationEmailRenderer _renderer;
    private readonly IEmailSender _emailSender;
    private readonly IClock _clock;
    private readonly ILogger<CashflowAlertJob> _logger;

    public CashflowAlertJob(
        INotificationSettingsRepository settings,
        ICashflowRepository cashflow,
        INotificationDispatchLog dispatchLog,
        NotificationEmailRenderer renderer,
        IEmailSender emailSender,
        IClock clock,
        ILogger<CashflowAlertJob> logger)
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
            .ListEnabledAcrossAllTenantsAsync(NotificationKind.CashflowAlert, cancellationToken)
            .ConfigureAwait(false);

        if (recipients.Count == 0)
        {
            _logger.LogInformation("Cashflow alert: nobody has it enabled");
            return;
        }

        var today = _clock.Today;
        var horizon = today.AddDays(HorizonDays);

        foreach (var recipient in recipients)
        {
            var forecasts = await _cashflow
                .ListStoredForecastsAsync([recipient.BusinessId], today, horizon, cancellationToken)
                .ConfigureAwait(false);

            if (forecasts.Count == 0)
                continue;

            var lowest = forecasts.MinBy(f => f.PredictedBalance)!;

            // Threshold zero means "tell me when it is predicted to go negative",
            // which is why the comparison is inclusive.
            if (lowest.PredictedBalance > recipient.CashflowAlertThreshold)
                continue;

            // Deliberately not keyed by date: the key IS the cooldown. One alert
            // per business per week, however long the dip persists.
            var dispatchKey = $"cashflow:{recipient.SettingsId}";

            if (await _dispatchLog
                .WasSentAsync(recipient.TenantId, dispatchKey, AlertCooldown, cancellationToken)
                .ConfigureAwait(false))
            {
                _logger.LogDebug(
                    "Cashflow alert for business {BusinessId} still within cooldown",
                    recipient.BusinessId);
                continue;
            }

            var email = _renderer.Render(new CashflowAlertEmail.Model(
                recipient.BusinessId,
                recipient.BusinessName,
                lowest.PredictedBalance,
                lowest.ForecastDate,
                recipient.CashflowAlertThreshold));

            await _emailSender
                .SendEmailAsync(recipient.Email, email.Subject, email.HtmlBody, cancellationToken)
                .ConfigureAwait(false);

            await _dispatchLog
                .RecordSentAsync(recipient.TenantId, dispatchKey, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Sent cashflow alert to {Recipient} for business {BusinessId}: {Balance} on {Date}",
                recipient.Email, recipient.BusinessId, lowest.PredictedBalance, lowest.ForecastDate);
        }
    }
}
