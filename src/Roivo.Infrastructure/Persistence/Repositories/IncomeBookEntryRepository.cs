using Microsoft.EntityFrameworkCore;
using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;

namespace Roivo.Infrastructure.Persistence.Repositories;

public sealed class IncomeBookEntryRepository : IIncomeBookEntryRepository
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public IncomeBookEntryRepository(IDbContextFactory<ApplicationDbContext> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    public async Task UpsertAsync(IncomeBookEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        // The unique index is (BusinessId, CounterpartyAfm, IssueDate,
        // DocumentTypeCode) with no TenantId, so a filtered probe under the
        // tenant-less cron misses and the insert violates it.
        var existing = await db.IncomeBookEntries
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e =>
                e.BusinessId == entry.BusinessId
                && e.CounterpartyAfm == entry.CounterpartyAfm
                && e.IssueDate == entry.IssueDate
                && e.DocumentTypeCode == entry.DocumentTypeCode, cancellationToken);

        if (existing is null)
        {
            db.IncomeBookEntries.Add(entry);
        }
        else
        {
            existing.UpdateTotals(
                entry.NetValue, entry.VatAmount, entry.GrossValue,
                entry.InvoiceCount, entry.MinMark, entry.MaxMark);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IncomeBookEntry?> GetByIdentityAsync(
        Guid businessId,
        string counterpartyAfm,
        DateTime issueDate,
        string documentTypeCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(counterpartyAfm);
        ArgumentNullException.ThrowIfNull(documentTypeCode);

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        // Business id already scopes this to one tenant; the filter only
        // breaks the tenant-less cron. See UpsertAsync.
        return await db.IncomeBookEntries
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e =>
                e.BusinessId == businessId
                && e.CounterpartyAfm == counterpartyAfm
                && e.IssueDate == issueDate
                && e.DocumentTypeCode == documentTypeCode, cancellationToken);
    }

    public async Task<int> CountByBusinessAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.IncomeBookEntries
            .AsNoTracking()
            .CountAsync(e => e.BusinessId == businessId, cancellationToken);
    }
}
