namespace Roivo.Application.Features.Reconciliation.Commands.RejectSuggestedMatch;

public sealed record RejectSuggestedMatchCommand(Guid MatchId, string UserId, string? Notes = null);
