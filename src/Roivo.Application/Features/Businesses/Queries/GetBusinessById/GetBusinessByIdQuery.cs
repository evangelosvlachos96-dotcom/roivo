namespace Roivo.Application.Features.Businesses.Queries.GetBusinessById;

public sealed record GetBusinessByIdQuery(Guid Id, bool ActiveOnly = true);
