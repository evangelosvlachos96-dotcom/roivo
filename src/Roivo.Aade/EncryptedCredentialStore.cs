using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roivo.Application.Abstractions.Aade;
using Roivo.Infrastructure.Persistence;

namespace Roivo.Aade;

/// <summary>
/// <see cref="IAadeCredentialStore"/> backed by EF Core; uses ASP.NET Data
/// Protection to encrypt both userId and subscriptionKey at rest. Keys live in
/// the platform-managed key ring (configured by the host).
/// </summary>
public sealed class EncryptedCredentialStore : IAadeCredentialStore
{
    private const string ProtectionPurpose = "Roivo.Aade.Credentials";

    private readonly IDataProtector _protector;
    private readonly IDbContextFactory<ApplicationDbContext> _factory;
    private readonly ILogger<EncryptedCredentialStore> _logger;

    public EncryptedCredentialStore(
        IDataProtectionProvider protectionProvider,
        IDbContextFactory<ApplicationDbContext> factory,
        ILogger<EncryptedCredentialStore> logger)
    {
        ArgumentNullException.ThrowIfNull(protectionProvider);
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(logger);

        _protector = protectionProvider.CreateProtector(ProtectionPurpose);
        _factory = factory;
        _logger = logger;
    }

    public async Task StoreAsync(Guid businessId, string userId, string subscriptionKey, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(subscriptionKey);

        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var business = await db.Businesses
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Business {businessId} not found.");

        business.AadeUserIdEncrypted = _protector.Protect(userId);
        business.AadeSubscriptionKeyEncrypted = _protector.Protect(subscriptionKey);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<AadeCredentials?> RetrieveAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var pair = await db.Businesses
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(b => b.Id == businessId)
            .Select(b => new { b.AadeUserIdEncrypted, b.AadeSubscriptionKeyEncrypted })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (pair is null
            || string.IsNullOrEmpty(pair.AadeUserIdEncrypted)
            || string.IsNullOrEmpty(pair.AadeSubscriptionKeyEncrypted))
        {
            return null;
        }

        try
        {
            var userId = _protector.Unprotect(pair.AadeUserIdEncrypted);
            var key = _protector.Unprotect(pair.AadeSubscriptionKeyEncrypted);
            return new AadeCredentials(userId, key);
        }
        catch (System.Security.Cryptography.CryptographicException ex)
        {
            // Key-ring drift or tampered cipher-text. Surface as "no credentials"
            // so the caller goes through the reconnect flow.
            _logger.LogError(ex, "Failed to unprotect AADE credentials for business {BusinessId}", businessId);
            return null;
        }
    }

    public async Task ClearAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var business = await db.Businesses
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken)
            .ConfigureAwait(false);
        if (business is null) return;

        business.AadeUserIdEncrypted = null;
        business.AadeSubscriptionKeyEncrypted = null;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> HasCredentialsAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.Businesses
            .AsNoTracking()
            .IgnoreQueryFilters()
            .AnyAsync(b => b.Id == businessId
                        && b.AadeUserIdEncrypted != null
                        && b.AadeSubscriptionKeyEncrypted != null, cancellationToken)
            .ConfigureAwait(false);
    }
}
