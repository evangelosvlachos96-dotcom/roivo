namespace Roivo.Application.Features.Aade.Queries.ListConnectedBusinesses;

/// <summary>
/// Returns the IDs of active businesses that have AADE credentials connected.
/// Used by the nightly sync cron to fan out per-business sync jobs.
/// </summary>
public sealed record ListConnectedBusinessesQuery;
