using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Tests.Fakes;

public sealed class FakeInvoiceRepository : IInvoiceRepository
{
    public Dictionary<string, Invoice> ByMark { get; } = new();

    public Task<Invoice?> GetByAadeMarkAsync(string mark, CancellationToken cancellationToken = default)
        => Task.FromResult(ByMark.TryGetValue(mark, out var i) ? i : null);

    public Task<Invoice> UpsertAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        ByMark[invoice.AadeMark] = invoice;
        return Task.FromResult(invoice);
    }

    public Task<int> CountByBusinessAsync(Guid businessId, CancellationToken cancellationToken = default)
        => Task.FromResult(ByMark.Values.Count(i => i.BusinessId == businessId));
}
