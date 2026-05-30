namespace Roivo.Aade.Configuration;

public sealed class AadeSettings
{
    /// <summary>AADE myDATA base URL. Configured per environment in appsettings.</summary>
    public required string BaseUrl { get; init; }

    /// <summary>Overall request timeout. Default 10s — short enough to surface
    /// failures quickly, long enough to ride out a slow AADE response.</summary>
    public required int TimeoutSeconds { get; init; } = 10;

    /// <summary>TCP connect timeout. Default 5s so DNS/network failures surface
    /// in under a second rather than waiting out the full request timeout.</summary>
    public required int ConnectTimeoutSeconds { get; init; } = 5;

    public required int MaxRetries { get; init; } = 3;

    /// <summary>Path for fetching documents issued to the calling entity (incoming).</summary>
    public required string RequestDocsPath { get; init; } = "RequestDocs";

    /// <summary>Path for fetching documents issued by the calling entity (outgoing).</summary>
    public required string RequestMyIncomePath { get; init; } = "RequestMyIncome";

    /// <summary>
    /// Date used as dateFrom for RequestMyIncome, which requires a date parameter
    /// even with mark-based pagination. Pre-AADE-myDATA era so all history is returned.
    /// </summary>
    public required string IncomeEpochDate { get; init; } = "01/01/2015";
}
