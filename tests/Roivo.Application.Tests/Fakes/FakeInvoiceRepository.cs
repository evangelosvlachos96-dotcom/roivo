using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Tests.Fakes;

public sealed class FakeInvoiceRepository : IInvoiceRepository
{
    public Dictionary<string, Invoice> ByMark { get; } = new();

    public Task<Invoice> UpsertAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        ByMark[invoice.AadeMark] = invoice;
        return Task.FromResult(invoice);
    }
}
