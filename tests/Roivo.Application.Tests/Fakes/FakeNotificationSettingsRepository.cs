using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Tests.Fakes;

/// <summary>
/// In-memory <see cref="INotificationSettingsRepository"/>. Keyed the same way
/// the unique index is, so an upsert that would violate it here would violate it
/// in Postgres too.
/// </summary>
public sealed class FakeNotificationSettingsRepository : INotificationSettingsRepository
{
    public Dictionary<(Guid BusinessId, string UserId), NotificationSettings> Store { get; } = new();

    /// <summary>Confirmed email per user id. A missing entry means "no confirmed address".</summary>
    public Dictionary<string, string> Emails { get; } = new(StringComparer.Ordinal);

    public List<NotificationRecipient> Recipients { get; } = new();

    public int UpsertCount { get; private set; }

    public Task<NotificationSettings?> GetAsync(
        Guid businessId, string userId, CancellationToken cancellationToken = default)
        => Task.FromResult(Store.TryGetValue((businessId, userId), out var s) ? s : null);

    public Task<NotificationSettings> UpsertAsync(
        NotificationSettings settings, CancellationToken cancellationToken = default)
    {
        UpsertCount++;
        Store[(settings.BusinessId, settings.UserId)] = settings;
        return Task.FromResult(settings);
    }

    public Task<IReadOnlyList<NotificationRecipient>> ListEnabledAcrossAllTenantsAsync(
        NotificationKind kind, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<NotificationRecipient> list = [.. Recipients];
        return Task.FromResult(list);
    }

    public Task<string?> GetConfirmedEmailAsync(
        string userId, CancellationToken cancellationToken = default)
        => Task.FromResult(Emails.TryGetValue(userId, out var email) ? email : null);
}
