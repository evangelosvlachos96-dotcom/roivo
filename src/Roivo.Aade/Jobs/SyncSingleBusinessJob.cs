using Microsoft.Extensions.Logging;
using Roivo.Application.Features.Aade.Commands.SyncBusinessInvoices;

namespace Roivo.Aade.Jobs;

/// <summary>
/// Per-business sync job. Hangfire enqueues one of these per connected business
/// from <see cref="NightlyAadeSyncJob"/>. Throws on transient failures so
/// Hangfire's exponential-backoff retry takes over.
/// </summary>
public sealed class SyncSingleBusinessJob
{
    private readonly SyncBusinessInvoicesHandler _handler;
    private readonly ILogger<SyncSingleBusinessJob> _logger;

    public SyncSingleBusinessJob(
        SyncBusinessInvoicesHandler handler,
        ILogger<SyncSingleBusinessJob> logger)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(logger);

        _handler = handler;
        _logger = logger;
    }

    public async Task Execute(Guid businessId)
    {
        var result = await _handler.Handle(new SyncBusinessInvoicesCommand(businessId)).ConfigureAwait(false);

        switch (result)
        {
            case SyncBusinessInvoicesResult.Success ok:
                _logger.LogInformation(
                    "AADE sync OK for business {BusinessId}: new={New} updated={Updated}",
                    businessId, ok.NewCount, ok.UpdatedCount);
                break;
            case SyncBusinessInvoicesResult.InvalidCredentials:
                // No point retrying — credentials are bad. Log a warning and stop.
                _logger.LogWarning("AADE credentials rejected for business {BusinessId}; user must reconnect", businessId);
                break;
            case SyncBusinessInvoicesResult.NotConnected:
                _logger.LogWarning("Business {BusinessId} no longer connected; skipping", businessId);
                break;
            case SyncBusinessInvoicesResult.BusinessNotFound:
                _logger.LogWarning("Business {BusinessId} not found; skipping", businessId);
                break;
            case SyncBusinessInvoicesResult.NetworkError ne:
                // Throw so Hangfire schedules a retry with exponential backoff.
                throw new InvalidOperationException($"AADE network error: {ne.Message}");
            case SyncBusinessInvoicesResult.AadeServerError se:
                throw new InvalidOperationException($"AADE returned {se.StatusCode}: {se.Message}");
        }
    }
}
