namespace Roivo.Application.Features.Aade.Commands.ConnectAade;

public sealed record ConnectAadeCommand(Guid BusinessId, string UserId, string SubscriptionKey);
