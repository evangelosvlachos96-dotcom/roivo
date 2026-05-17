namespace Roivo.Application.Features.Businesses.Queries.ListActiveBusinesses;

/// <summary>
/// Lists active businesses for the current tenant. Tenant scope is enforced
/// by the persistence layer via global query filters.
/// </summary>
public sealed record ListActiveBusinessesQuery;
