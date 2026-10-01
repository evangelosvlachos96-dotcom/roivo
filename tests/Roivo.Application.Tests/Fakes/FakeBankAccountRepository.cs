using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Tests.Fakes;

public sealed class FakeBankAccountRepository : IBankAccountRepository
{
    public Dictionary<Guid, BankAccount> Store { get; } = new();

    public Task<IReadOnlyList<BankAccount>> ListByBusinessAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<BankAccount> list = Store.Values.Where(a => a.BusinessId == businessId).ToList();
        return Task.FromResult(list);
    }

    public Task<IReadOnlyList<BankAccount>> ListByBusinessAcrossAllTenantsAsync(Guid businessId, CancellationToken cancellationToken = default)
        => ListByBusinessAsync(businessId, cancellationToken);

    public Task<BankAccount> UpsertAsync(BankAccount account, CancellationToken cancellationToken = default)
    {
        var existing = Store.Values.FirstOrDefault(
            a => a.BusinessId == account.BusinessId && a.ExternalAccountUid == account.ExternalAccountUid);

        if (existing is null)
        {
            Store[account.Id] = account;
            return Task.FromResult(account);
        }

        existing.BankName = account.BankName;
        existing.Iban = account.Iban;
        existing.Currency = account.Currency;
        return Task.FromResult(existing);
    }

    public Task DeleteByBusinessAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        foreach (var id in Store.Where(kv => kv.Value.BusinessId == businessId).Select(kv => kv.Key).ToList())
            Store.Remove(id);

        return Task.CompletedTask;
    }
}
