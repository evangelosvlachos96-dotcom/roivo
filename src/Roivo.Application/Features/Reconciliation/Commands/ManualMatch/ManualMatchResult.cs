namespace Roivo.Application.Features.Reconciliation.Commands.ManualMatch;

/// <summary>Outcomes of <see cref="ManualMatchHandler"/>.</summary>
public abstract record ManualMatchResult
{
    public sealed record Success(Guid MatchId) : ManualMatchResult;
    public sealed record Forbidden(string Reason) : ManualMatchResult;

    /// <summary>The invoice, the transaction, or both do not belong to this business.</summary>
    public sealed record NotFound : ManualMatchResult;

    /// <summary>A non-rejected match already ties this exact pair together.</summary>
    public sealed record AlreadyMatched : ManualMatchResult;

    /// <summary>One side is already reconciled against something else.</summary>
    public sealed record SideAlreadyReconciled : ManualMatchResult;
}
