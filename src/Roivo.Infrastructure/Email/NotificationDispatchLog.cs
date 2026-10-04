using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roivo.Application.Abstractions;
using Roivo.Application.Features.Notifications;
using Roivo.Core.Domain.Auditing;
using Roivo.Core.Domain.Entities;
using Roivo.Infrastructure.Persistence;

namespace Roivo.Infrastructure.Email;

/// <summary>
/// <see cref="INotificationDispatchLog"/> backed by the <c>AuditLogs</c> table.
/// </summary>
/// <remarks>
/// <para>
/// WHY THE AUDIT TABLE: the notification jobs need durable "already sent"
/// memory keyed by things that have no column anywhere — a specific tax
/// obligation at a specific lead time, a digest for one calendar day, a cooldown
/// on a cashflow alert. <c>NotificationSettings</c> has no <c>LastSentAt</c>
/// field and its migration is frozen, so there is no dedicated place to put
/// them. <c>AuditLogs</c> is already the durable append-only record of
/// everything the system does, and a notification going out is genuinely an
/// audit-worthy event, so the ledger and the audit trail are the same rows.
/// </para>
/// <para>
/// The arbitrary string key is folded into <c>AuditLog.EntityId</c> (a
/// <see cref="Guid"/>) by hashing, and kept verbatim in <c>Details</c> so the
/// row is still readable in pgAdmin. Lookups are always scoped by tenant and by
/// timestamp so they ride the existing <c>(TenantId, Timestamp)</c> index rather
/// than scanning the table.
/// </para>
/// <para>
/// THE PROPER FIX, when the schema can move again: a small
/// <c>NotificationDispatches</c> table — <c>(TenantId, DispatchKey, SentAt)</c>
/// with a unique index on <c>(TenantId, DispatchKey)</c> — which would also turn
/// the check-then-send race into a single atomic insert. Swapping this class for
/// one backed by that table changes nothing above it.
/// </para>
/// </remarks>
public sealed class NotificationDispatchLog : INotificationDispatchLog
{
    private const string DispatchEntityType = "NotificationDispatch";

    private static readonly string SentAction = nameof(AuditAction.NotificationEmailSent);

    private readonly IDbContextFactory<ApplicationDbContext> _factory;
    private readonly IClock _clock;
    private readonly ILogger<NotificationDispatchLog> _logger;

    public NotificationDispatchLog(
        IDbContextFactory<ApplicationDbContext> factory,
        IClock clock,
        ILogger<NotificationDispatchLog> logger)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(logger);

        _factory = factory;
        _clock = clock;
        _logger = logger;
    }

    public async Task<bool> WasSentAsync(
        Guid tenantId,
        string dispatchKey,
        TimeSpan window,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dispatchKey);

        var cutoff = _clock.UtcNow - window;
        var keyId = ToKeyId(dispatchKey);

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        // AuditLog is not ITenantScoped, so there is no query filter to bypass
        // here — the TenantId predicate is ours, for the index.
        return await db.AuditLogs
            .AsNoTracking()
            .AnyAsync(
                a => a.TenantId == tenantId
                    && a.Timestamp >= cutoff
                    && a.Action == SentAction
                    && a.EntityId == keyId,
                cancellationToken);
    }

    public async Task RecordSentAsync(
        Guid tenantId,
        string dispatchKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dispatchKey);

        try
        {
            await using var db = await _factory.CreateDbContextAsync(cancellationToken);

            db.AuditLogs.Add(new AuditLog
            {
                Action = SentAction,
                EntityType = DispatchEntityType,
                EntityId = ToKeyId(dispatchKey),
                TenantId = tenantId,
                Details = dispatchKey,
                Timestamp = _clock.UtcNow,
            });

            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // Same contract as IAuditWriter: failing to write the ledger must not
            // fail the job. The cost is that the next run may resend this one
            // message, which is the right way to be wrong.
            _logger.LogError(ex, "Failed to record notification dispatch {DispatchKey}", dispatchKey);
        }
    }

    /// <summary>
    /// Folds an arbitrary dispatch key into the fixed-width uuid column.
    /// SHA-256 truncated to 16 bytes: not a security boundary, just a stable
    /// 128-bit identity with collision odds far below anything that matters at
    /// this row count.
    /// </summary>
    private static Guid ToKeyId(string dispatchKey)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(dispatchKey));
        return new Guid(hash.AsSpan(0, 16));
    }
}
