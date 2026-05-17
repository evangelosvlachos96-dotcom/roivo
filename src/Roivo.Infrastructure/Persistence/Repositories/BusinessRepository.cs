using Microsoft.EntityFrameworkCore;
using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;

namespace Roivo.Infrastructure.Persistence.Repositories;

public sealed class BusinessRepository : IBusinessRepository
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    // The repository owns short-lived contexts. The pending-state for AddAsync
    // is kept on a single context spanning Add → SaveChanges to satisfy callers
    // that follow the "stage then save" pattern from the abstraction.
    private ApplicationDbContext? _pending;

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

    public async Task<bool> AfmExistsAsync(string afm, bool activeOnly, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(afm);

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var query = db.Businesses.AsNoTracking().Where(b => b.Afm == afm);
        if (activeOnly) query = query.Where(b => b.IsActive);
        if (excludeId is Guid id) query = query.Where(b => b.Id != id);
        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Business business, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(business);

        // Lazily open a tracked context for the staged add. SaveChangesAsync
        // will commit and dispose it.
        _pending ??= await _factory.CreateDbContextAsync(cancellationToken);
        await _pending.Businesses.AddAsync(business, cancellationToken);
    }

    public async Task UpdateAsync(Business business, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(business);

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        db.Businesses.Update(business);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_pending is null) return;

        try
        {
            await _pending.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            await _pending.DisposeAsync();
            _pending = null;
        }
    }
}
