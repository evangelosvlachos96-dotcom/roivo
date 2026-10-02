using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Features.Cashflow.Commands.AddRecurringItem;

public sealed record AddRecurringItemCommand(
    Guid BusinessId,
    string Name,
    decimal Amount,
    CashflowCategoryType Type,
    int? RecurringDay);
