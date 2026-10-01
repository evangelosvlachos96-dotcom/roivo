using Roivo.Application.Abstractions.Banking;

namespace Roivo.Application.Tests.Fakes;

public sealed class FakeBankingCredentialStore : IBankingCredentialStore
{
    public Dictionary<Guid, string> Store { get; } = new();

    public Task StoreAsync(Guid businessId, string sessionId, CancellationToken cancellationToken = default)
    {
        Store[businessId] = sessionId;
        return Task.CompletedTask;
    }

    public Task<string?> RetrieveAsync(Guid businessId, CancellationToken cancellationToken = default)
        => Task.FromResult(Store.TryGetValue(businessId, out var s) ? s : null);

    public Task ClearAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        Store.Remove(businessId);
        return Task.CompletedTask;
    }

    public Task<bool> HasCredentialsAsync(Guid businessId, CancellationToken cancellationToken = default)
        => Task.FromResult(Store.ContainsKey(businessId));
}
