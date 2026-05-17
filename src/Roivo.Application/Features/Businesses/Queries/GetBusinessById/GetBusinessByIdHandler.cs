using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Features.Businesses.Queries.GetBusinessById;

public sealed class GetBusinessByIdHandler
{
    private readonly IBusinessRepository _repository;

    public GetBusinessByIdHandler(IBusinessRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public Task<Business?> Handle(GetBusinessByIdQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return query.ActiveOnly
            ? _repository.GetByIdActiveOnlyAsync(query.Id, cancellationToken)
            : _repository.GetByIdAsync(query.Id, cancellationToken);
    }
}
