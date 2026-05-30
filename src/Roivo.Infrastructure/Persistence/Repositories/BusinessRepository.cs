using Microsoft.EntityFrameworkCore;
using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;

namespace Roivo.Infrastructure.Persistence.Repositories;

public sealed class BusinessRepository : IBusinessRepository
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public BusinessRepository(IDbContextFactory<ApplicationDbContext> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    public async Task<Business?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<Business?> GetByIdActiveOnlyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id && b.IsActive, cancellationToken);
    }

    public async Task<Business?> FindByAfmAsync(string afm, bool activeOnly, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(afm);

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var query = db.Businesses.AsNoTracking().Where(b => b.Afm == afm);
        if (activeOnly) query = query.Where(b => b.IsActive);
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Business>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.Businesses
            .AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> ListIdsWithAadeCredentialsAcrossAllTenantsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        // IgnoreQueryFilters because the cron runs without a tenant scope.
        return await db.Businesses
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(b => b.IsActive
                     && b.AadeUserIdEncrypted != null
                     && b.AadeSubscriptionKeyEncrypted != null)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> AfmExistsAsync(string afm, bool activeOnly, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(afm);

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var query = db.Businesses.AsNoTracking().Where(b => b.Afm == afm);
        if (activeOnly) query = query.Where(b => b.IsActive);
        if (excludeId is Guid id) query = query.Where(b => b.Id != id);
        return await query.AnyAsync(cancellationToken);
    }

    public async Task<Business> AddAsync(Business business, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(business);

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        await db.Businesses.AddAsync(business, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return business;
    }

    public async Task<Business> UpdateAsync(Business business, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(business);

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        db.Businesses.Update(business);
        await db.SaveChangesAsync(cancellationToken);
        return business;
    }

    public async Task<IReadOnlyList<Business>> ListWithExpiredAadeFailureAsync(
        DateTime threshold,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        // IgnoreQueryFilters because the notification cron runs without a tenant scope.
        return await db.Businesses
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(b => b.IsActive
                     && b.HasAadeFailure
                     && b.AadeLastFailureAt != null
                     && b.AadeLastFailureAt < threshold
                     && b.AadeFailureEmailSentAt == null)
            .ToListAsync(cancellationToken);
    }

    public async Task<string?> GetTenantPrimaryEmailAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        // Earliest-created user is treated as the tenant owner. EmailConfirmed
        // is required so we never email an address the user hasn't proven.
        return await db.Users
            .AsNoTracking()
            .Where(u => u.TenantId == tenantId && u.EmailConfirmed && u.Email != null)
            .OrderBy(u => u.CreatedAt)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
