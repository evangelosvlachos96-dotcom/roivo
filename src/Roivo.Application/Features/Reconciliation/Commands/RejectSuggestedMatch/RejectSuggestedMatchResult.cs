namespace Roivo.Application.Features.Reconciliation.Commands.RejectSuggestedMatch;

/// <summary>Outcomes of <see cref="RejectSuggestedMatchHandler"/>.</summary>
public abstract record RejectSuggestedMatchResult
{
    public sealed record Success : RejectSuggestedMatchResult;
    public sealed record Forbidden(string Reason) : RejectSuggestedMatchResult;
    public sealed record NotFound : RejectSuggestedMatchResult;
    public sealed record NotPending(string Status) : RejectSuggestedMatchResult;
}
