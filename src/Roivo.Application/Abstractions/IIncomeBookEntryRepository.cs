using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Abstractions;

/// <summary>
/// Persistence boundary for <see cref="IncomeBookEntry"/> (aggregated outgoing
/// summaries from AADE's RequestMyIncome). Same stateless-factory pattern as
/// the other repositories.
/// </summary>
public interface IIncomeBookEntryRepository
{
    /// <summary>
    /// Inserts a new aggregate or updates the existing one matching the
    /// identity tuple (BusinessId, CounterpartyAfm, IssueDate, DocumentTypeCode).
    /// </summary>
    Task UpsertAsync(IncomeBookEntry entry, CancellationToken cancellationToken = default);

    Task<IncomeBookEntry?> GetByIdentityAsync(
        Guid businessId,
        string counterpartyAfm,
        DateTime issueDate,
        string documentTypeCode,
        CancellationToken cancellationToken = default);

    Task<int> CountByBusinessAsync(Guid businessId, CancellationToken cancellationToken = default);
}
