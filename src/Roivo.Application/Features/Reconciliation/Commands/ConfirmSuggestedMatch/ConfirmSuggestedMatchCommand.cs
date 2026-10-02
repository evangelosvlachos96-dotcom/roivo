namespace Roivo.Application.Features.Reconciliation.Commands.ConfirmSuggestedMatch;

public sealed record ConfirmSuggestedMatchCommand(Guid MatchId, string UserId);
