using Hangfire;
using Microsoft.Extensions.Logging;
using Roivo.Application.Features.Aade.Queries.ListConnectedBusinesses;

namespace Roivo.Aade.Jobs;

/// <summary>
/// Hangfire-invoked recurring job. Lists every connected business across all
/// tenants and enqueues a per-business sync. Runs at 03:00 Europe/Athens daily.
/// </summary>
public sealed class NightlyAadeSyncJob
{
    private readonly ListConnectedBusinessesHandler _list;
    private readonly IBackgroundJobClient _jobs;
    private readonly ILogger<NightlyAadeSyncJob> _logger;

    public NightlyAadeSyncJob(
        ListConnectedBusinessesHandler list,
        IBackgroundJobClient jobs,
        ILogger<NightlyAadeSyncJob> logger)
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
        var ids = await _list.Handle(new ListConnectedBusinessesQuery()).ConfigureAwait(false);
        _logger.LogInformation("Nightly AADE sync: enqueueing {Count} businesses", ids.Count);

        foreach (var id in ids)
        {
            _jobs.Enqueue<SyncSingleBusinessJob>(j => j.Execute(id));
        }
    }
}
