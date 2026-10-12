namespace Roivo.Application.Features.Reconciliation.Queries.GetUnreconciledItems;

/// <summary>Which side of the two-panel matching view to page through.</summary>
public enum UnreconciledItemType
{
    Invoices = 1,
    Transactions = 2,
}

public sealed record GetUnreconciledItemsQuery(
    Guid BusinessId,
    UnreconciledItemType ItemType,
    int Page = 1,
    int PageSize = 25);

/// <summary>One unreconciled row, shaped the same whichever side it came from.</summary>
public sealed record UnreconciledItem(
    Guid Id,
    DateOnly Date,
    decimal Amount,
    string? CounterpartyName,
    string? Description);

public sealed record UnreconciledItemsPage(
    UnreconciledItemType ItemType,
    IReadOnlyList<UnreconciledItem> Items,
    int Page,
    int PageSize);
