using Hangfire;
using Microsoft.Extensions.Logging;
using Roivo.Application.Features.Banking.Queries.ListBankingConnectedBusinesses;

namespace Roivo.Banking.Jobs;

/// <summary>
/// Hangfire-invoked recurring job. Lists every bank-connected business across
/// all tenants and enqueues a per-business sync. Runs at 04:00 Europe/Athens
/// daily — an hour after the AADE sync, so the two don't contend.
/// </summary>
public sealed class NightlyBankingSyncJob
{
    private readonly ListBankingConnectedBusinessesHandler _list;
    private readonly IBackgroundJobClient _jobs;
    private readonly ILogger<NightlyBankingSyncJob> _logger;

    public NightlyBankingSyncJob(
        ListBankingConnectedBusinessesHandler list,
        IBackgroundJobClient jobs,
        ILogger<NightlyBankingSyncJob> logger)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(jobs);
        ArgumentNullException.ThrowIfNull(logger);

        _list = list;
        _jobs = jobs;
        _logger = logger;
    }

    // Hangfire requires public, parameterless (or simple-args) entry points.
    public async Task Execute()
    {
        var ids = await _list.Handle(new ListBankingConnectedBusinessesQuery()).ConfigureAwait(false);
        _logger.LogInformation("Starting nightly banking sync for {Count} businesses", ids.Count);

        foreach (var id in ids)
        {
            _jobs.Enqueue<SyncSingleBusinessBankingJob>(j => j.Execute(id));
        }
    }
}
