using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Tests.Fakes;

public sealed class FakeBusinessRepository : IBusinessRepository
{
    public Dictionary<Guid, Business> Store { get; } = new();

    // Returns the stored reference directly. With the rich entity having
    // private setters, deep-cloning would require reflection — and tests
    // currently flow data through repository methods (which is realistic
    // enough). If a test starts caring about detached-copy semantics, add
    // a reflection-based clone here.
    public Task<Business?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Store.TryGetValue(id, out var b) ? b : null);

    public Task<Business?> GetByIdActiveOnlyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!Store.TryGetValue(id, out var b)) return Task.FromResult<Business?>(null);
        return Task.FromResult<Business?>(b.IsActive ? b : null);
    }

    public Task<Business?> FindByAfmAsync(string afm, bool activeOnly, CancellationToken cancellationToken = default)
    {
        var match = Store.Values.FirstOrDefault(b => b.Afm == afm && (!activeOnly || b.IsActive));
        return Task.FromResult(match);
    }

    public Task<IReadOnlyList<Business>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Business> list = Store.Values
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .ToList();
        return Task.FromResult(list);
    }

    public Task<IReadOnlyList<Guid>> ListIdsWithAadeCredentialsAcrossAllTenantsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Guid> ids = Store.Values
            .Where(b => b.IsActive
                     && !string.IsNullOrEmpty(b.AadeUserIdEncrypted)
                     && !string.IsNullOrEmpty(b.AadeSubscriptionKeyEncrypted))
            .Select(b => b.Id)
            .ToList();
        return Task.FromResult(ids);
    }

    public Task<bool> AfmExistsAsync(string afm, bool activeOnly, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var exists = Store.Values.Any(b =>
            b.Afm == afm
            && (!activeOnly || b.IsActive)
            && (excludeId is null || b.Id != excludeId));
        return Task.FromResult(exists);
    }

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
}
