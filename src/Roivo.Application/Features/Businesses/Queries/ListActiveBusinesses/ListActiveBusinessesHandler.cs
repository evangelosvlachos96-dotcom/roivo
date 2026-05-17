using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Features.Businesses.Queries.ListActiveBusinesses;

public sealed class ListActiveBusinessesHandler
{
    private readonly IBusinessRepository _repository;

    public ListActiveBusinessesHandler(IBusinessRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public Task<IReadOnlyList<Business>> Handle(ListActiveBusinessesQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return _repository.ListActiveAsync(cancellationToken);
    }
}
