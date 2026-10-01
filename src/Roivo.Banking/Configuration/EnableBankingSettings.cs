using System.ComponentModel.DataAnnotations;

namespace Roivo.Banking.Configuration;

/// <summary>
/// Enable Banking connection settings. The three secret-ish values
/// (<see cref="BaseUrl"/>, <see cref="ApplicationId"/>,
/// <see cref="PrivateKeyPath"/>) come from the local <c>.env</c> file via
/// <see cref="DotEnvFile"/>; the rest are non-secret defaults in appsettings.
/// </summary>
public sealed class EnableBankingSettings
{
    /// <summary>API root, e.g. <c>https://api.enablebanking.com</c>.</summary>
    [Required]
    public required string BaseUrl { get; init; }

    /// <summary>
    /// Enable Banking application id. Signed JWTs carry it as the <c>kid</c>
    /// header so Enable Banking can pick the right public key.
    /// </summary>
    [Required]
    public required string ApplicationId { get; init; }

    /// <summary>Path to the PEM-encoded RSA private key registered with the
    /// application. Relative paths resolve against the content root.</summary>
    [Required]
    public required string PrivateKeyPath { get; init; }

    /// <summary>JWT <c>iss</c> claim. Fixed by Enable Banking.</summary>
    public string Issuer { get; init; } = "enablebanking.com";

    /// <summary>JWT <c>aud</c> claim. Fixed by Enable Banking.</summary>
    public string Audience { get; init; } = "api.enablebanking.com";

    /// <summary>
    /// JWT lifetime. Enable Banking caps this at 24h; we mint a short-lived
    /// token per call instead, so a leaked token expires quickly.
    /// </summary>
    [Range(30, 86400)]
    public int TokenLifetimeSeconds { get; init; } = 300;

    /// <summary>ISO-3166 country whose banks we offer. Roivo is Greece-only.</summary>
    public string Country { get; init; } = "GR";

    /// <summary>
    /// How long to ask the bank to keep the consent alive. PSD2 caps
    /// unattended access at 90 days, after which the user must re-consent.
    /// </summary>
    [Range(1, 180)]
    public int ConsentValidDays { get; init; } = 90;

    /// <summary>How far back a sync reaches. Matches <see cref="ConsentValidDays"/>
    /// so a freshly reconnected account backfills its whole consent window.</summary>
    [Range(1, 730)]
    public int SyncLookbackDays { get; init; } = 90;

    /// <summary>Overall request timeout.</summary>
    [Range(1, 120)]
    public int TimeoutSeconds { get; init; } = 30;

    /// <summary>TCP connect timeout, so DNS/network failures surface fast
    /// instead of waiting out the full request timeout.</summary>
    [Range(1, 60)]
    public int ConnectTimeoutSeconds { get; init; } = 5;

    [Range(0, 10)]
    public int MaxRetries { get; init; } = 3;

    /// <summary>
    /// Safety valve on transaction pagination so a malformed
    /// <c>continuation_key</c> loop can't spin forever.
    /// </summary>
    [Range(1, 1000)]
    public int MaxTransactionPages { get; init; } = 50;

    /// <summary>
    /// Where Enable Banking sends the user back after consent. Must be
    /// registered with the application on Enable Banking's side, or the
    /// redirect is refused.
    /// </summary>
    [Required]
    public required string RedirectUrl { get; init; }
}
