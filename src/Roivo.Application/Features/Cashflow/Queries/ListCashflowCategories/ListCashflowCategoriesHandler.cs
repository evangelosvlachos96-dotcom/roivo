using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Features.Cashflow.Queries.ListCashflowCategories;

public sealed class ListCashflowCategoriesHandler
{
    private readonly ICashflowRepository _repository;

    public ListCashflowCategoriesHandler(ICashflowRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public async Task<CashflowCategoryList> Handle(
        ListCashflowCategoriesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var categories = await _repository
            .ListCategoriesAsync(query.BusinessId, cancellationToken)
            .ConfigureAwait(false);

        return new CashflowCategoryList(
            Income: [.. categories.Where(c => c.Type == CashflowCategoryType.Income)],
            Expense: [.. categories.Where(c => c.Type == CashflowCategoryType.Expense)]);
    }
}
