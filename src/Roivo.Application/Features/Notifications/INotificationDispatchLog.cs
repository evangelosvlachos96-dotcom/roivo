namespace Roivo.Application.Features.Notifications;

/// <summary>
/// Durable record of which notification emails have already gone out, so a
/// recurring job can run every night without mailing the same thing twice.
/// </summary>
/// <remarks>
/// <para>
/// Mirrors the "email sent at" stamp on <c>Business</c> that the AADE and
/// banking failure jobs use, but keyed by an arbitrary string because the
/// notification jobs dedupe on things that have no column of their own: a
/// specific tax obligation at a specific lead time, a digest for a given day, a
/// cashflow alert cooldown.
/// </para>
/// <para>
/// <c>NotificationSettings</c> deliberately carries no <c>LastSentAt</c> field
/// (the entity and its migration are frozen), so the shipped implementation
/// writes the ledger into <c>AuditLogs</c>. See
/// <c>NotificationDispatchLog</c> for the trade-off and the schema change that
/// would replace it.
/// </para>
/// </remarks>
public interface INotificationDispatchLog
{
    /// <summary>
    /// True when <paramref name="dispatchKey"/> was already recorded within
    /// <paramref name="window"/> of now. Jobs check this before sending.
    /// </summary>
    /// <param name="tenantId">
    /// Scopes the lookup so it rides the <c>(TenantId, Timestamp)</c> index
    /// rather than scanning the whole audit table.
    /// </param>
    /// <param name="window">
    /// How far back to look. Doubles as a cooldown: a seven-day window on a
    /// cashflow alert means at most one such alert per week.
    /// </param>
    Task<bool> WasSentAsync(
        Guid tenantId,
        string dispatchKey,
        TimeSpan window,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records that the email for <paramref name="dispatchKey"/> was sent.
    /// Called after a successful send, following the same send-then-stamp order
    /// as the AADE and banking failure jobs: a transport failure leaves no
    /// stamp, so the next run retries rather than silently dropping the message.
    /// </summary>
    Task RecordSentAsync(
        Guid tenantId,
        string dispatchKey,
        CancellationToken cancellationToken = default);
}
