namespace Roivo.Application.Features.Cashflow.Commands.AddRecurringItem;

/// <summary>Outcomes of <see cref="AddRecurringItemHandler"/>.</summary>
public abstract record AddRecurringItemResult
{
    public sealed record Success(Guid CategoryId) : AddRecurringItemResult;
    public sealed record Forbidden(string Reason) : AddRecurringItemResult;
    public sealed record NotFound : AddRecurringItemResult;

    /// <summary>Name empty, day out of range, or amount negative.</summary>
    public sealed record Invalid(string Reason) : AddRecurringItemResult;
}
