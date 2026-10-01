namespace Roivo.Application.Features.Banking.Queries.GetBankingConnectionStatus;

public sealed record GetBankingConnectionStatusQuery(Guid BusinessId);

/// <summary>
/// Snapshot of a business's bank connection for UI rendering. <c>State</c>
/// collapses the underlying fields into the three screens the UI actually has,
/// so the component doesn't re-derive the rule.
/// </summary>
public sealed record BankingConnectionInfo(
    BankingConnectionState State,
    string? BankName,
    DateTime? LastSyncAt,
    DateTime? ConsentExpiresAt,
    int ErrorCount,
    string? LastFailureReason,
    IReadOnlyList<BankingAccountSummary> Accounts);

public enum BankingConnectionState
{
    Disconnected = 0,
    Connected = 1,

    /// <summary>Connected, but syncs are failing persistently — the user has
    /// something to fix.</summary>
    Error = 2,
}

public sealed record BankingAccountSummary(Guid Id, string BankName, string Iban, string Currency);
