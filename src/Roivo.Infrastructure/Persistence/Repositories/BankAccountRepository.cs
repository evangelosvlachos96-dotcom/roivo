using Microsoft.EntityFrameworkCore;
using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;

namespace Roivo.Infrastructure.Persistence.Repositories;

public sealed class BankAccountRepository : IBankAccountRepository
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public BankAccountRepository(IDbContextFactory<ApplicationDbContext> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    public async Task<IReadOnlyList<BankAccount>> ListByBusinessAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.BankAccounts
            .AsNoTracking()
            .Where(a => a.BusinessId == businessId)
            .OrderBy(a => a.Iban)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BankAccount>> ListByBusinessAcrossAllTenantsAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        // IgnoreQueryFilters because the sync cron runs without a tenant scope.
        return await db.BankAccounts
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(a => a.BusinessId == businessId)
            .OrderBy(a => a.Iban)
            .ToListAsync(cancellationToken);
    }

    public async Task<BankAccount> UpsertAsync(BankAccount account, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var existing = await db.BankAccounts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                a => a.BusinessId == account.BusinessId && a.ExternalAccountUid == account.ExternalAccountUid,
                cancellationToken);

        if (existing is null)
        {
            db.BankAccounts.Add(account);
            await db.SaveChangesAsync(cancellationToken);
            return account;
        }

        existing.BankName = account.BankName;
        existing.Iban = account.Iban;
        existing.Currency = account.Currency;
        // CurrentBalance and LastSyncedAt are owned by the sync job, not by the
        // consent flow — leave whatever the last sync wrote.

        await db.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task DeleteByBusinessAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        // IgnoreQueryFilters so a disconnect issued from a background context
        // (no tenant scope) still finds the rows. Transactions go first
        // explicitly rather than by relying on the FK's cascade, so the intent
        // survives a future change to the relationship's delete behaviour.
        var accountIds = await db.BankAccounts
            .IgnoreQueryFilters()
            .Where(a => a.BusinessId == businessId)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        if (accountIds.Count == 0) return;

        await db.BankTransactions
            .IgnoreQueryFilters()
            .Where(t => accountIds.Contains(t.BankAccountId))
            .ExecuteDeleteAsync(cancellationToken);

        await db.BankAccounts
            .IgnoreQueryFilters()
            .Where(a => a.BusinessId == businessId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
