namespace Roivo.Application.Abstractions.Aade;

/// <summary>
/// Persists AADE credentials per business. Implementations encrypt the values
/// at rest — handlers always go through this abstraction, never touching the
/// underlying columns directly.
/// </summary>
public interface IAadeCredentialStore
{
    Task StoreAsync(Guid businessId, string userId, string subscriptionKey, CancellationToken cancellationToken = default);
    Task<AadeCredentials?> RetrieveAsync(Guid businessId, CancellationToken cancellationToken = default);
    Task ClearAsync(Guid businessId, CancellationToken cancellationToken = default);
    Task<bool> HasCredentialsAsync(Guid businessId, CancellationToken cancellationToken = default);
}

public sealed record AadeCredentials(string UserId, string SubscriptionKey);
