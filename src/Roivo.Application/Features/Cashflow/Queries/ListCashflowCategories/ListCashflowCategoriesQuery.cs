using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Features.Cashflow.Queries.ListCashflowCategories;

public sealed record ListCashflowCategoriesQuery(Guid BusinessId);

/// <summary>
/// A business's categories split by direction, so the UI does not have to
/// partition them to render the two lists and the breakdown chart.
/// </summary>
public sealed record CashflowCategoryList(
    IReadOnlyList<CashflowCategory> Income,
    IReadOnlyList<CashflowCategory> Expense)
{
    /// <summary>Total of the recurring income items that carry an amount.</summary>
    public decimal MonthlyIncomeTotal => Income.Where(c => c.IsRecurring).Sum(c => c.AverageAmount ?? 0m);

    /// <summary>Total of the recurring expense items that carry an amount.</summary>
    public decimal MonthlyExpenseTotal => Expense.Where(c => c.IsRecurring).Sum(c => c.AverageAmount ?? 0m);

    public int TotalCount => Income.Count + Expense.Count;
}
