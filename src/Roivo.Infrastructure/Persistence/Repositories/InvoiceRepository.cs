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
            // Invoice has private setters (rich entity); copy mutable scalar
            // columns through EF's change tracker rather than property setters.
            // Id / TenantId / CreatedAt are intentionally excluded so the
            // original row identity and audit timestamp are preserved.
            db.Entry(existing).CurrentValues.SetValues(new Dictionary<string, object?>
            {
                [nameof(Invoice.BusinessId)] = invoice.BusinessId,
                [nameof(Invoice.Direction)] = invoice.Direction,
                [nameof(Invoice.InvoiceType)] = invoice.InvoiceType,
                [nameof(Invoice.IssueDate)] = invoice.IssueDate,
                [nameof(Invoice.CounterpartyAfm)] = invoice.CounterpartyAfm,
                [nameof(Invoice.CounterpartyName)] = invoice.CounterpartyName,
                [nameof(Invoice.NetAmount)] = invoice.NetAmount,
                [nameof(Invoice.VatAmount)] = invoice.VatAmount,
                [nameof(Invoice.GrossAmount)] = invoice.GrossAmount,
                [nameof(Invoice.Currency)] = invoice.Currency,
                [nameof(Invoice.CancelledByMark)] = invoice.CancelledByMark,
            });
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
