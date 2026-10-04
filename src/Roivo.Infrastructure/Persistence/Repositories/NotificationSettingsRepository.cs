using Microsoft.EntityFrameworkCore;
using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Enums;

namespace Roivo.Infrastructure.Persistence.Repositories;

public sealed class NotificationSettingsRepository : INotificationSettingsRepository
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public NotificationSettingsRepository(IDbContextFactory<ApplicationDbContext> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    public async Task<NotificationSettings?> GetAsync(
        Guid businessId, string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        return await db.NotificationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.BusinessId == businessId && n.UserId == userId, cancellationToken);
    }

    public async Task<NotificationSettings> UpsertAsync(
        NotificationSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        // Read methods return detached entities, so "is this an insert or an
        // update" cannot be answered from the instance. Ask the database, keyed
        // on the pair the unique index covers.
        var exists = await db.NotificationSettings
            .AnyAsync(n => n.Id == settings.Id, cancellationToken);

        if (exists)
            db.NotificationSettings.Update(settings);
        else
            db.NotificationSettings.Add(settings);

        await db.SaveChangesAsync(cancellationToken);
        return settings;
    }

    public async Task<IReadOnlyList<NotificationRecipient>> ListEnabledAcrossAllTenantsAsync(
        NotificationKind kind, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        // IgnoreQueryFilters is mandatory here: the caller is a Hangfire job with
        // no ambient tenant, so the global filter would compare TenantId against
        // Guid.Empty and return an empty list without erroring. Both sides of the
        // join need it — the Businesses filter would otherwise re-apply it.
        var query = db.NotificationSettings
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(EnabledFor(kind))
            .Join(
                db.Businesses.IgnoreQueryFilters().Where(b => b.IsActive),
                n => n.BusinessId,
                b => b.Id,
                (n, b) => new
                {
                    n.Id,
                    n.TenantId,
                    n.BusinessId,
                    BusinessName = b.Name,
                    n.UserId,
                    n.TaxReminderDaysBefore,
                    n.CashflowAlertThreshold,
                });

        var rows = await query.ToListAsync(cancellationToken);
        if (rows.Count == 0)
            return [];

        // NotificationSettings.UserId is the string form of ApplicationUser.Id
        // (Identity's own convention). Parsing here instead of translating
        // Guid.ToString() into SQL keeps the comparison on the indexed uuid
        // column rather than a cast.
        var userIds = rows
            .Select(r => Guid.TryParse(r.UserId, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (userIds.Count == 0)
            return [];

        var emails = await db.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id) && u.EmailConfirmed && u.Email != null)
            .Select(u => new { u.Id, u.Email })
            .ToDictionaryAsync(u => u.Id, u => u.Email!, cancellationToken);

        var recipients = new List<NotificationRecipient>(rows.Count);
        foreach (var row in rows)
        {
            if (!Guid.TryParse(row.UserId, out var userId))
                continue;

            // An unconfirmed address is not a send target. Dropping the row here
            // rather than in SQL keeps the email lookup to one query.
            if (!emails.TryGetValue(userId, out var email))
                continue;

            recipients.Add(new NotificationRecipient(
                row.Id,
                row.TenantId,
                row.BusinessId,
                row.BusinessName,
                row.UserId,
                email,
                row.TaxReminderDaysBefore,
                row.CashflowAlertThreshold));
        }

        return recipients;
    }

    public async Task<string?> GetConfirmedEmailAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        if (!Guid.TryParse(userId, out var id))
            return null;

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        return await db.Users
            .AsNoTracking()
            .Where(u => u.Id == id && u.EmailConfirmed && u.Email != null)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static System.Linq.Expressions.Expression<Func<NotificationSettings, bool>> EnabledFor(
        NotificationKind kind) => kind switch
        {
            NotificationKind.DailyDigest => n => n.DailyDigestEnabled,
            NotificationKind.TaxReminder => n => n.TaxReminderEnabled,
            NotificationKind.CashflowAlert => n => n.CashflowAlertEnabled,
            NotificationKind.SyncFailure => n => n.SyncFailureAlertEnabled,
            NotificationKind.WeeklyReconciliation => n => n.WeeklyReconciliationEnabled,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown notification kind."),
        };
}
