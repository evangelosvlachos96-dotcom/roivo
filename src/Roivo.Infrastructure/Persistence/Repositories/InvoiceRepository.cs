using Microsoft.EntityFrameworkCore;
using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;

namespace Roivo.Infrastructure.Persistence.Repositories;

public sealed class InvoiceRepository : IInvoiceRepository
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public InvoiceRepository(IDbContextFactory<ApplicationDbContext> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    public async Task<Invoice?> GetByAadeMarkAsync(string mark, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mark);

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.Invoices
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.AadeMark == mark, cancellationToken);
    }

    public async Task<Invoice> UpsertAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var existing = await db.Invoices
            .FirstOrDefaultAsync(i => i.AadeMark == invoice.AadeMark, cancellationToken);

        if (existing is null)
        {
            db.Invoices.Add(invoice);
        }
        else
        {
            existing.BusinessId = invoice.BusinessId;
            existing.Direction = invoice.Direction;
            existing.InvoiceType = invoice.InvoiceType;
            existing.IssueDate = invoice.IssueDate;
            existing.CounterpartyAfm = invoice.CounterpartyAfm;
            existing.CounterpartyName = invoice.CounterpartyName;
            existing.GrossAmount = invoice.GrossAmount;
            existing.Currency = invoice.Currency;
            existing.RawPayload = invoice.RawPayload;
        }

        await db.SaveChangesAsync(cancellationToken);
        return existing ?? invoice;
    }

    public async Task<int> CountByBusinessAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.Invoices
            .AsNoTracking()
            .CountAsync(i => i.BusinessId == businessId, cancellationToken);
    }
}
