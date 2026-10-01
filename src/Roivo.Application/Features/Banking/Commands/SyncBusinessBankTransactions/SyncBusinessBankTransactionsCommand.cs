namespace Roivo.Application.Features.Banking.Commands.SyncBusinessBankTransactions;

/// <summary>
/// Syncs one business's bank transactions.
/// </summary>
/// <param name="BusinessId">The business to sync.</param>
/// <param name="BypassTenantScope">
/// Set only by the nightly cron, which runs system-wide with no signed-in user
/// and would otherwise be filtered out of every row by the tenant query filter.
/// A request-scoped caller must leave this false, or a user could sync another
/// tenant's business by guessing its id.
/// </param>
public sealed record SyncBusinessBankTransactionsCommand(Guid BusinessId, bool BypassTenantScope = false);
