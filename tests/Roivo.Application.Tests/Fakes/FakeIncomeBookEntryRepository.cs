using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Tests.Fakes;

public sealed class FakeIncomeBookEntryRepository : IIncomeBookEntryRepository
{
    // Keyed by the AADE identity tuple.
    public Dictionary<(Guid BusinessId, string CounterpartyAfm, DateTime IssueDate, string DocumentTypeCode), IncomeBookEntry> Store { get; } = new();

    public Task UpsertAsync(IncomeBookEntry entry, CancellationToken cancellationToken = default)
    {
        var key = (entry.BusinessId, entry.CounterpartyAfm, entry.IssueDate, entry.DocumentTypeCode);
        Store[key] = entry;
        return Task.CompletedTask;
    }

    public Task<IncomeBookEntry?> GetByIdentityAsync(
        Guid businessId,
        string counterpartyAfm,
        DateTime issueDate,
        string documentTypeCode,
        CancellationToken cancellationToken = default)
    {
        Store.TryGetValue((businessId, counterpartyAfm, issueDate, documentTypeCode), out var entry);
        return Task.FromResult(entry);
    }

    public Task<int> CountByBusinessAsync(Guid businessId, CancellationToken cancellationToken = default)
        => Task.FromResult(Store.Values.Count(e => e.BusinessId == businessId));
}
