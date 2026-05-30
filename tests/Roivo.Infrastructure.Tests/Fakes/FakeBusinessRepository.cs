using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;

namespace Roivo.Infrastructure.Tests.Fakes;

/// <summary>
/// In-memory <see cref="IBusinessRepository"/> for Infrastructure-layer tests.
/// Mirrors the shape used in Application tests so behaviour is consistent —
/// not shared via project reference to keep test projects independent.
/// </summary>
public sealed class FakeBusinessRepository : IBusinessRepository
{
    public Dictionary<Guid, Business> Store { get; } = new();
    public Dictionary<Guid, string?> TenantPrimaryEmails { get; } = new();

    public Task<Business?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Store.TryGetValue(id, out var b) ? b : null);

    public Task<Business?> GetByIdActiveOnlyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!Store.TryGetValue(id, out var b)) return Task.FromResult<Business?>(null);
        return Task.FromResult<Business?>(b.IsActive ? b : null);
    }

    public Task<Business?> FindByAfmAsync(string afm, bool activeOnly, CancellationToken cancellationToken = default)
        => Task.FromResult(Store.Values.FirstOrDefault(b => b.Afm == afm && (!activeOnly || b.IsActive)));

    public Task<IReadOnlyList<Business>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Business> list = Store.Values.Where(b => b.IsActive).ToList();
        return Task.FromResult(list);
    }

    public Task<IReadOnlyList<Guid>> ListIdsWithAadeCredentialsAcrossAllTenantsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Guid> list = Store.Values
            .Where(b => b.IsActive && !string.IsNullOrEmpty(b.AadeUserIdEncrypted) && !string.IsNullOrEmpty(b.AadeSubscriptionKeyEncrypted))
            .Select(b => b.Id)
            .ToList();
        return Task.FromResult(list);
    }

    public Task<bool> AfmExistsAsync(string afm, bool activeOnly, Guid? excludeId = null, CancellationToken cancellationToken = default)
        => Task.FromResult(Store.Values.Any(b => b.Afm == afm && (!activeOnly || b.IsActive) && (excludeId is null || b.Id != excludeId)));

    public Task<Business> AddAsync(Business business, CancellationToken cancellationToken = default)
    {
        Store[business.Id] = business;
        return Task.FromResult(business);
    }

    public Task<Business> UpdateAsync(Business business, CancellationToken cancellationToken = default)
    {
        Store[business.Id] = business;
        return Task.FromResult(business);
    }

    public Task<IReadOnlyList<Business>> ListWithExpiredAadeFailureAsync(DateTime threshold, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Business> list = Store.Values
            .Where(b => b.IsActive
                     && b.HasAadeFailure
                     && b.AadeLastFailureAt.HasValue
                     && b.AadeLastFailureAt < threshold
                     && b.AadeFailureEmailSentAt is null)
            .ToList();
        return Task.FromResult(list);
    }

    public Task<string?> GetTenantPrimaryEmailAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => Task.FromResult(TenantPrimaryEmails.TryGetValue(tenantId, out var email) ? email : null);
}
