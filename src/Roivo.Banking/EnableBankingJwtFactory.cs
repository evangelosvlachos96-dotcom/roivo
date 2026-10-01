using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Roivo.Banking.Configuration;

namespace Roivo.Banking;

/// <summary>
/// Mints the RS256 JWTs Enable Banking uses instead of API keys. One token is
/// minted per request and lives only minutes, so there is nothing long-lived
/// to leak.
/// </summary>
/// <remarks>
/// Hand-rolled rather than via <c>System.IdentityModel.Tokens.Jwt</c>: the
/// token is three base64url segments and an RSA signature, and avoiding the
/// dependency keeps the key material in one visible place.
/// </remarks>
public sealed class EnableBankingJwtFactory : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly EnableBankingSettings _settings;
    private readonly RSA _rsa;
    private bool _disposed;

    public EnableBankingJwtFactory(IOptions<EnableBankingSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings.Value;
        _rsa = LoadPrivateKey(_settings.PrivateKeyPath);
    }

    /// <summary>
    /// Returns a signed bearer token valid for
    /// <see cref="EnableBankingSettings.TokenLifetimeSeconds"/>.
    /// </summary>
    public string CreateToken(DateTimeOffset? now = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var issuedAt = now ?? DateTimeOffset.UtcNow;
        var expiresAt = issuedAt.AddSeconds(_settings.TokenLifetimeSeconds);

        var header = new Dictionary<string, object>
        {
            ["typ"] = "JWT",
            ["alg"] = "RS256",
            ["kid"] = _settings.ApplicationId,
        };

        var payload = new Dictionary<string, object>
        {
            ["iss"] = _settings.Issuer,
            ["aud"] = _settings.Audience,
            ["iat"] = issuedAt.ToUnixTimeSeconds(),
            ["exp"] = expiresAt.ToUnixTimeSeconds(),
        };

        var signingInput = $"{EncodeSegment(header)}.{EncodeSegment(payload)}";
        var signature = _rsa.SignData(
            Encoding.ASCII.GetBytes(signingInput),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        return $"{signingInput}.{Base64UrlEncode(signature)}";
    }

    private static string EncodeSegment(object value)
        => Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions));

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>
    /// Loads a PEM private key. Enable Banking's console hands out PKCS#8
    /// ("BEGIN PRIVATE KEY"), but keys converted with older openssl builds come
    /// out PKCS#1 ("BEGIN RSA PRIVATE KEY"); <c>ImportFromPem</c> reads both.
    /// </summary>
    private static RSA LoadPrivateKey(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var resolved = Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(path, Directory.GetCurrentDirectory());

        if (!File.Exists(resolved))
        {
            throw new FileNotFoundException(
                $"Enable Banking private key not found at '{resolved}'. Set EnableBanking:PrivateKeyPath (or ENABLE_BANKING_PRIVATE_KEY_PATH in .env).",
                resolved);
        }

        var rsa = RSA.Create();
        try
        {
            rsa.ImportFromPem(File.ReadAllText(resolved));
        }
        catch (ArgumentException ex)
        {
            rsa.Dispose();
            throw new InvalidOperationException($"Enable Banking private key at '{resolved}' is not a readable PEM private key.", ex);
        }
        catch (CryptographicException ex)
        {
            rsa.Dispose();
            throw new InvalidOperationException($"Enable Banking private key at '{resolved}' could not be imported.", ex);
        }

        return rsa;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _rsa.Dispose();
    }
}
