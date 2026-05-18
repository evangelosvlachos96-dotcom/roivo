namespace Roivo.Aade.Configuration;

public sealed class AadeSettings
{
    /// <summary>AADE myDATA base URL — dev host for M4 (<c>https://mydata-dev.azure-api.net</c>).</summary>
    public required string BaseUrl { get; init; }

    public int TimeoutSeconds { get; init; } = 30;

    public int MaxRetries { get; init; } = 3;
}
