namespace Roivo.Application.Features.Aade.Queries.GetAadeConnectionStatus;

public sealed record GetAadeConnectionStatusQuery(Guid BusinessId);

public sealed record AadeConnectionStatus(
    bool IsConnected,
    DateTime? LastSyncAt,
    string? MaskedUserId);
