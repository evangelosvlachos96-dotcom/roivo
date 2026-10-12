using Roivo.Application.Features.Reconciliation.Queries.GetReconciliationDashboard;
using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Features.Reconciliation.Queries.ListReconciliationMatches;

/// <summary>
/// A page of a business's reconciliation history. A null <c>Status</c> lists
/// every status.
/// </summary>
public sealed record ListReconciliationMatchesQuery(
    Guid BusinessId,
    ReconciliationMatchStatus? Status = null,
    int Page = 1,
    int PageSize = 25);

/// <summary>
/// One page of matches plus the total, so the UI can page server-side instead
/// of pulling the whole history and filtering in the browser.
/// </summary>
public sealed record ReconciliationMatchesPage(
    IReadOnlyList<ReconciliationMatchListItem> Items,
    int Page,
    int PageSize,
    int TotalCount);
