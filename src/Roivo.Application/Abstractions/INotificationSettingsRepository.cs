using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Abstractions;

/// <summary>
/// Persistence boundary for <see cref="NotificationSettings"/>. Same
/// stateless-factory pattern as <see cref="IBusinessRepository"/>: every call
/// opens and disposes its own context and returned entities are detached.
/// </summary>
public interface INotificationSettingsRepository
{
    /// <summary>
    /// The preferences stored for one user on one business, or null when the
    /// user has never saved any. Tenant-filtered — request-scoped callers only.
    /// </summary>
    Task<NotificationSettings?> GetAsync(
        Guid businessId,
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts or updates the row for (BusinessId, UserId). The unique index on
    /// that pair is what makes this safe to call without reading first.
    /// </summary>
    Task<NotificationSettings> UpsertAsync(
        NotificationSettings settings,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Every user across every tenant who has <paramref name="kind"/> switched
    /// on, joined to the business name and the user's confirmed email address.
    /// </summary>
    /// <remarks>
    /// Bypasses the tenant query filter. A Hangfire job has no signed-in user,
    /// so <c>ITenantContext.CurrentTenantId</c> is <see cref="Guid.Empty"/> and a
    /// filtered query matches nothing at all — silently, with no error. That is
    /// exactly how the AADE cron ran as a no-op for weeks. Never call this from
    /// a request-scoped path: it is a deliberate hole in tenant isolation.
    /// </remarks>
    Task<IReadOnlyList<NotificationRecipient>> ListEnabledAcrossAllTenantsAsync(
        NotificationKind kind,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The user's confirmed email address, or null when the account has none
    /// (unconfirmed or missing). Notifications never go to an unproven address.
    /// </summary>
    Task<string?> GetConfirmedEmailAsync(
        string userId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// One row of "who to email, about which business, with which thresholds" —
/// everything a notification job needs for one send, in one projection, so the
/// jobs do not fan out into per-recipient lookups.
/// </summary>
/// <param name="SettingsId">Identifies the preference row, used as the dispatch-dedupe key.</param>
/// <param name="Email">A confirmed address; the query never returns unconfirmed ones.</param>
public sealed record NotificationRecipient(
    Guid SettingsId,
    Guid TenantId,
    Guid BusinessId,
    string BusinessName,
    string UserId,
    string Email,
    int TaxReminderDaysBefore,
    decimal CashflowAlertThreshold);
