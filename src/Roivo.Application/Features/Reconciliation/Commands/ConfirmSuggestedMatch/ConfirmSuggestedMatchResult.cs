namespace Roivo.Application.Features.Reconciliation.Commands.ConfirmSuggestedMatch;

/// <summary>Outcomes of <see cref="ConfirmSuggestedMatchHandler"/>.</summary>
public abstract record ConfirmSuggestedMatchResult
{
    public sealed record Success : ConfirmSuggestedMatchResult;
    public sealed record Forbidden(string Reason) : ConfirmSuggestedMatchResult;
    public sealed record NotFound : ConfirmSuggestedMatchResult;

    /// <summary>The match had already been decided; <paramref name="Status"/> is its current state.</summary>
    public sealed record NotPending(string Status) : ConfirmSuggestedMatchResult;
}
