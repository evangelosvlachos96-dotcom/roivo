using Roivo.Application.Features.Reconciliation.Services;

namespace Roivo.Application.Features.Reconciliation.Commands.RunReconciliation;

/// <summary>Outcomes of <see cref="RunReconciliationHandler"/>.</summary>
public abstract record RunReconciliationResult
{
    /// <remarks>
    /// <c>PersistedMatches</c> is how many rows were actually inserted — lower
    /// than the engine's match count when a concurrent run claimed some pairs.
    /// </remarks>
    public sealed record Success(ReconciliationResult Result, int PersistedMatches) : RunReconciliationResult;

    public sealed record Forbidden(string Reason) : RunReconciliationResult;
    public sealed record NotFound : RunReconciliationResult;
    public sealed record InvalidDateRange : RunReconciliationResult;
}
