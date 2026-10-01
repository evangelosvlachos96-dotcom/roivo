using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Roivo.Banking;
using Roivo.Banking.Configuration;

namespace Roivo.Banking.Tests;

public sealed class EnableBankingJwtFactoryTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("roivo-jwt-").FullName;
    private readonly RSA _key = RSA.Create(2048);

    [Fact]
    public void CreateToken_ProducesAThreeSegmentTokenWithTheExpectedHeader()
    {
        using var factory = CreateFactory(WriteKeyPem());

        var segments = factory.CreateToken().Split('.');

        segments.Should().HaveCount(3);

        var header = DecodeSegment(segments[0]);
        header.GetProperty("typ").GetString().Should().Be("JWT");
        header.GetProperty("alg").GetString().Should().Be("RS256");
        header.GetProperty("kid").GetString().Should().Be("app-123");
    }

    [Fact]
    public void CreateToken_SetsTheClaimsEnableBankingRequires()
    {
        var now = new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);
        using var factory = CreateFactory(WriteKeyPem());

        var payload = DecodeSegment(factory.CreateToken(now).Split('.')[1]);

        payload.GetProperty("iss").GetString().Should().Be("enablebanking.com");
        payload.GetProperty("aud").GetString().Should().Be("api.enablebanking.com");
        payload.GetProperty("iat").GetInt64().Should().Be(now.ToUnixTimeSeconds());
        payload.GetProperty("exp").GetInt64().Should().Be(now.AddSeconds(300).ToUnixTimeSeconds());
    }

    [Fact]
    public void CreateToken_SignatureVerifiesAgainstThePublicKey()
    {
        using var factory = CreateFactory(WriteKeyPem());

        var token = factory.CreateToken();
        var lastDot = token.LastIndexOf('.');
        var signingInput = Encoding.ASCII.GetBytes(token[..lastDot]);
        var signature = Base64UrlDecode(token[(lastDot + 1)..]);

        _key.VerifyData(signingInput, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            .Should().BeTrue();
    }

    [Fact]
    public void CreateToken_MintsAFreshTokenEachCall()
    {
        using var factory = CreateFactory(WriteKeyPem());

        var first = factory.CreateToken(new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero));
        var second = factory.CreateToken(new DateTimeOffset(2026, 9, 30, 12, 5, 0, TimeSpan.Zero));

        second.Should().NotBe(first);
    }

    [Fact]
    public void Constructor_ReadsPkcs1PemAsWellAsPkcs8()
    {
        var pkcs1Path = Path.Combine(_directory, "pkcs1.pem");
        File.WriteAllText(pkcs1Path, _key.ExportRSAPrivateKeyPem());

        using var factory = CreateFactory(pkcs1Path);

        factory.CreateToken().Split('.').Should().HaveCount(3);
    }

    [Fact]
    public void Constructor_ThrowsWhenTheKeyFileIsMissing()
    {
        var act = () => CreateFactory(Path.Combine(_directory, "absent.pem"));

        act.Should().Throw<FileNotFoundException>()
            .WithMessage("*ENABLE_BANKING_PRIVATE_KEY_PATH*");
    }

    [Fact]
    public void Constructor_ThrowsWhenTheFileIsNotAPemKey()
    {
        var path = Path.Combine(_directory, "garbage.pem");
        File.WriteAllText(path, "this is not a key");

        var act = () => CreateFactory(path);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not a readable PEM private key*");
    }

    private string WriteKeyPem()
    {
        var path = Path.Combine(_directory, "key.pem");
        File.WriteAllText(path, _key.ExportPkcs8PrivateKeyPem());
        return path;
    }

    private static EnableBankingJwtFactory CreateFactory(string keyPath)
        => new(Options.Create(new EnableBankingSettings
        {
            BaseUrl = "https://api.enablebanking.com",
            ApplicationId = "app-123",
            PrivateKeyPath = keyPath,
            RedirectUrl = "https://localhost/banking/callback",
        }));

    private static JsonElement DecodeSegment(string segment)
        => JsonDocument.Parse(Base64UrlDecode(segment)).RootElement.Clone();

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += new string('=', (4 - padded.Length % 4) % 4);
        return Convert.FromBase64String(padded);
    }

    public void Dispose()
    {
        _key.Dispose();
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }
}
