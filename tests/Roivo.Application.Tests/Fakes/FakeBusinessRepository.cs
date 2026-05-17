using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Tests.Fakes;

public sealed class FakeBusinessRepository : IBusinessRepository
{
    public Dictionary<Guid, Business> Store { get; } = new();

    public Task<Business?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Store.TryGetValue(id, out var b) ? Clone(b) : null);

    public Task<Business?> GetByIdActiveOnlyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!Store.TryGetValue(id, out var b)) return Task.FromResult<Business?>(null);
        return Task.FromResult<Business?>(b.IsActive ? Clone(b) : null);
    }

    public Task<Business?> FindByAfmAsync(string afm, bool activeOnly, CancellationToken cancellationToken = default)
    {
        var match = Store.Values.FirstOrDefault(b => b.Afm == afm && (!activeOnly || b.IsActive));
        return Task.FromResult(match is null ? null : Clone(match));
    }

    public Task<IReadOnlyList<Business>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Business> list = Store.Values
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .Select(Clone)
            .ToList();
        return Task.FromResult(list);
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
        Store[business.Id] = Clone(business);
        return Task.FromResult(business);
    }

    public Task<Business> UpdateAsync(Business business, CancellationToken cancellationToken = default)
    {
        Store[business.Id] = Clone(business);
        return Task.FromResult(business);
    }

    // Detach from caller mutations so tests assert on a stable snapshot.
    private static Business Clone(Business b) => new()
    {
        Id = b.Id,
        TenantId = b.TenantId,
        Name = b.Name,
        Afm = b.Afm,
        Kad = b.Kad,
        Address = b.Address,
        CreatedAt = b.CreatedAt,
        AadeUserId = b.AadeUserId,
        IsActive = b.IsActive,
        AadeSubscriptionKeyEncrypted = b.AadeSubscriptionKeyEncrypted,
    };
}
