namespace Roivo.Application.Abstractions.Banking;

/// <summary>
/// Persists the aggregator session per business. Implementations encrypt the
/// value at rest — handlers always go through this abstraction, never touching
/// the underlying column directly.
/// </summary>
public interface IBankingCredentialStore
{
    Task StoreAsync(Guid businessId, string sessionId, CancellationToken cancellationToken = default);

    /// <summary>Returns the decrypted session id, or null when the business is
    /// not connected or the cipher-text can no longer be read.</summary>
    Task<string?> RetrieveAsync(Guid businessId, CancellationToken cancellationToken = default);

    Task ClearAsync(Guid businessId, CancellationToken cancellationToken = default);
    Task<bool> HasCredentialsAsync(Guid businessId, CancellationToken cancellationToken = default);
}
