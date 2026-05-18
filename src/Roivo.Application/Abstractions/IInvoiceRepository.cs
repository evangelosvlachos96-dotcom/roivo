using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Abstractions;

/// <summary>
/// Persistence boundary for <see cref="Invoice"/>. Same stateless-factory
/// pattern as <see cref="IBusinessRepository"/>.
/// </summary>
public interface IInvoiceRepository
{
    Task<Invoice?> GetByAadeMarkAsync(string mark, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts or updates the invoice. Match is by <see cref="Invoice.AadeMark"/>.
    /// Returns the persisted entity.
    /// </summary>
    Task<Invoice> UpsertAsync(Invoice invoice, CancellationToken cancellationToken = default);

    Task<int> CountByBusinessAsync(Guid businessId, CancellationToken cancellationToken = default);
}
