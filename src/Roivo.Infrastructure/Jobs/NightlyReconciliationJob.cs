using Microsoft.Extensions.Logging;
using Roivo.Application.Abstractions;
using Roivo.Application.Features.Reconciliation.Commands.RunReconciliation;

namespace Roivo.Infrastructure.Jobs;

/// <summary>
/// Hangfire-invoked recurring job. Reconciles every business that has both an
/// AADE connection and a bank connection. Runs at 05:00 Europe/Athens, an hour
/// after the banking sync, so it works on the night's freshly imported rows.
/// </summary>
/// <remarks>
/// Unlike the AADE and banking syncs this does not fan out to per-business
/// Hangfire jobs: reconciliation is pure computation against rows already in
/// Postgres, with no third-party rate limit to respect, so the fan-out would
/// add queue churn without buying isolation that matters.
/// </remarks>
public sealed class NightlyReconciliationJob
{
    /// <summary>Window reconciled each night. Matches the banking sync lookback.</summary>
    public const int LookbackDays = 90;

    private readonly IBusinessRepository _businesses;
    private readonly RunReconciliationHandler _handler;
    private readonly ILogger<NightlyReconciliationJob> _logger;

    public NightlyReconciliationJob(
        IBusinessRepository businesses,
        RunReconciliationHandler handler,
        ILogger<NightlyReconciliationJob> logger)
    {
        ArgumentNullException.ThrowIfNull(businesses);
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(logger);

        _businesses = businesses;
        _handler = handler;
        _logger = logger;
    }

    // Hangfire requires a public, parameterless entry point.
    public async Task Execute()
    {
        var banking = await _businesses.ListIdsWithBankingCredentialsAcrossAllTenantsAsync().ConfigureAwait(false);
        var aade = await _businesses.ListIdsWithAadeCredentialsAcrossAllTenantsAsync().ConfigureAwait(false);

        // Both sides are required: with only one connected there is nothing to
        // match against and every invoice would be reported as unreconciled.
        var eligible = banking.Intersect(aade).ToList();

        _logger.LogInformation(
            "Starting nightly reconciliation for {Count} businesses with both connections", eligible.Count);

        var to = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = to.AddDays(-LookbackDays);

        foreach (var businessId in eligible)
        {
            var result = await _handler
                .Handle(new RunReconciliationCommand(businessId, from, to, BypassTenantScope: true))
                .ConfigureAwait(false);

            switch (result)
            {
                case RunReconciliationResult.Success success:
                    _logger.LogInformation(
                        "Reconciled business {BusinessId}: {Auto} automatic, {Suggested} suggested, {Persisted} persisted",
                        businessId,
                        success.Result.Summary.AutoMatchedCount,
                        success.Result.Summary.SuggestedCount,
                        success.PersistedMatches);
                    break;

                case RunReconciliationResult.NotFound:
                    _logger.LogWarning("Business {BusinessId} not found during reconciliation; skipping", businessId);
                    break;

                default:
                    _logger.LogWarning(
                        "Reconciliation for business {BusinessId} returned {Result}",
                        businessId, result.GetType().Name);
                    break;
            }
        }
    }
}
