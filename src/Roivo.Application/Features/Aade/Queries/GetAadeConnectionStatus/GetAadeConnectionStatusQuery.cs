namespace Roivo.Application.Features.Aade.Queries.GetAadeConnectionStatus;

public sealed record GetAadeConnectionStatusQuery(Guid BusinessId);

/// <summary>
/// Snapshot of a business's AADE connection state for UI rendering. Connection
/// is binary because validation is synchronous and definitive — no
/// "pending verification" half-state.
/// </summary>
public sealed record AadeConnectionInfo(
    bool IsConnected,
    DateTime? LastSyncAt,
    string? MaskedUserId);
