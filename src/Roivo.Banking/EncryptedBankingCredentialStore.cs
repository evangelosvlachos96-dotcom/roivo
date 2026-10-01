using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roivo.Application.Abstractions.Banking;
using Roivo.Infrastructure.Persistence;

namespace Roivo.Banking;

/// <summary>
/// <see cref="IBankingCredentialStore"/> backed by EF Core; uses ASP.NET Data
/// Protection to encrypt the aggregator session id at rest. Keys live in the
/// platform-managed key ring (configured by the host).
/// </summary>
public sealed class EncryptedBankingCredentialStore : IBankingCredentialStore
{
    // Distinct from the AADE purpose string, so an AADE cipher-text can never be
    // unprotected as a banking one (or the reverse).
    private const string ProtectionPurpose = "Roivo.Banking.Credentials";

    private readonly IDataProtector _protector;
    private readonly IDbContextFactory<ApplicationDbContext> _factory;
    private readonly ILogger<EncryptedBankingCredentialStore> _logger;

    public EncryptedBankingCredentialStore(
        IDataProtectionProvider protectionProvider,
        IDbContextFactory<ApplicationDbContext> factory,
        ILogger<EncryptedBankingCredentialStore> logger)
    {
        ArgumentNullException.ThrowIfNull(protectionProvider);
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(logger);

        _protector = protectionProvider.CreateProtector(ProtectionPurpose);
        _factory = factory;
        _logger = logger;
    }

    public async Task StoreAsync(Guid businessId, string sessionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var business = await db.Businesses
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Business {businessId} not found.");

        business.BankingAccessTokenEncrypted = _protector.Protect(sessionId);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<string?> RetrieveAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var cipherText = await db.Businesses
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(b => b.Id == businessId)
            .Select(b => b.BankingAccessTokenEncrypted)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrEmpty(cipherText))
            return null;

        try
        {
            return _protector.Unprotect(cipherText);
        }
        catch (System.Security.Cryptography.CryptographicException ex)
        {
            // Key-ring drift or tampered cipher-text. Surface as "no credentials"
            // so the caller goes through the reconnect flow.
            _logger.LogError(ex, "Failed to unprotect banking session for business {BusinessId}", businessId);
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

        business.BankingAccessTokenEncrypted = null;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> HasCredentialsAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.Businesses
            .AsNoTracking()
            .IgnoreQueryFilters()
            .AnyAsync(b => b.Id == businessId && b.BankingAccessTokenEncrypted != null, cancellationToken)
            .ConfigureAwait(false);
    }
}
