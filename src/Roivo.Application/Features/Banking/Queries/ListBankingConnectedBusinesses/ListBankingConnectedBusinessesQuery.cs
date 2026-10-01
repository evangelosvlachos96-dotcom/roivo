namespace Roivo.Application.Features.Banking.Queries.ListBankingConnectedBusinesses;

/// <summary>
/// Returns the IDs of active businesses that have a bank connection. Used by the
/// nightly sync cron to fan out per-business sync jobs.
/// </summary>
public sealed record ListBankingConnectedBusinessesQuery;
