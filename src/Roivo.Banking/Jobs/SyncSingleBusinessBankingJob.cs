using Microsoft.Extensions.Logging;
using Roivo.Application.Features.Banking.Commands.SyncBusinessBankTransactions;

namespace Roivo.Banking.Jobs;

/// <summary>
/// Per-business banking sync. Hangfire enqueues one of these per connected
/// business from <see cref="NightlyBankingSyncJob"/>. Throws on transient
/// failures so Hangfire's exponential-backoff retry takes over; domain failures
/// log and return, because retrying a revoked consent achieves nothing.
/// </summary>
public sealed class SyncSingleBusinessBankingJob
{
    private readonly SyncBusinessBankTransactionsHandler _handler;
    private readonly ILogger<SyncSingleBusinessBankingJob> _logger;

    public SyncSingleBusinessBankingJob(
        SyncBusinessBankTransactionsHandler handler,
        ILogger<SyncSingleBusinessBankingJob> logger)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(logger);

        _handler = handler;
        _logger = logger;
    }

    public async Task Execute(Guid businessId)
    {
        // BypassTenantScope: the cron has no signed-in user, so the tenant query
        // filter would hide every business from it.
        var result = await _handler
            .Handle(new SyncBusinessBankTransactionsCommand(businessId, BypassTenantScope: true))
            .ConfigureAwait(false);

        switch (result)
        {
            case SyncBusinessBankTransactionsResult.Success ok:
                _logger.LogInformation(
                    "Banking sync OK for business {BusinessId}: new={New} updated={Updated} accounts={Accounts}",
                    businessId, ok.NewCount, ok.UpdatedCount, ok.AccountCount);
                break;
            case SyncBusinessBankTransactionsResult.SessionExpired:
                // The handler has already recorded the failure, which drives the
                // UI banner and the 24-hour notification. Nothing to retry.
                _logger.LogWarning("Banking consent expired for business {BusinessId}; user must reconnect", businessId);
                break;
            case SyncBusinessBankTransactionsResult.NotConnected:
                _logger.LogWarning("Business {BusinessId} has no usable bank connection; skipping", businessId);
                break;
            case SyncBusinessBankTransactionsResult.BusinessNotFound:
                _logger.LogWarning("Business {BusinessId} not found; skipping", businessId);
                break;
            case SyncBusinessBankTransactionsResult.NetworkError ne:
                throw new InvalidOperationException($"Banking network error for business {businessId}: {ne.Message}");
            case SyncBusinessBankTransactionsResult.BankingServerError se:
                throw new InvalidOperationException($"Banking API returned {se.StatusCode} for business {businessId}: {se.Message}");
        }
    }
}
