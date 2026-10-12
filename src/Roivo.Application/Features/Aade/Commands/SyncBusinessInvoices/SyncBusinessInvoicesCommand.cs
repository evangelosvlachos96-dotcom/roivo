namespace Roivo.Application.Features.Aade.Commands.SyncBusinessInvoices;

/// <summary>
/// Syncs one business's invoices from AADE.
/// </summary>
/// <remarks>
/// <c>BypassTenantScope</c> is set only by the nightly cron, which runs with no
/// signed-in user and therefore no ambient tenant — without it the
/// tenant-filtered lookup returns null for every business and the sync is a
/// silent no-op. Request-scoped callers must leave it false.
/// </remarks>
public sealed record SyncBusinessInvoicesCommand(Guid BusinessId, bool BypassTenantScope = false);
