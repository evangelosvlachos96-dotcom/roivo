using Roivo.Application.Abstractions.Aade;

namespace Roivo.Application.Tests.Fakes;

public sealed class FakeAadeCredentialStore : IAadeCredentialStore
{
    public Dictionary<Guid, AadeCredentials> Store { get; } = new();

    public Task StoreAsync(Guid businessId, string userId, string subscriptionKey, CancellationToken cancellationToken = default)
    {
        Store[businessId] = new AadeCredentials(userId, subscriptionKey);
        return Task.CompletedTask;
    }

    public Task<AadeCredentials?> RetrieveAsync(Guid businessId, CancellationToken cancellationToken = default)
        => Task.FromResult(Store.TryGetValue(businessId, out var c) ? c : null);

    public Task ClearAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        Store.Remove(businessId);
        return Task.CompletedTask;
    }

    public Task<bool> HasCredentialsAsync(Guid businessId, CancellationToken cancellationToken = default)
        => Task.FromResult(Store.ContainsKey(businessId));
}
