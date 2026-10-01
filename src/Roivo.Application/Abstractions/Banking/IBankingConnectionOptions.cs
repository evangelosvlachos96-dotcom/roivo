namespace Roivo.Application.Abstractions.Banking;

/// <summary>
/// The connection parameters a handler needs to start an authorization, without
/// binding Application to any aggregator's settings class. Implemented in the
/// aggregator project from its own configuration.
/// </summary>
public interface IBankingConnectionOptions
{
    /// <summary>ISO-3166 country whose banks we offer.</summary>
    string Country { get; }

    /// <summary>Where the bank sends the user back after consent.</summary>
    string RedirectUrl { get; }

    /// <summary>How long to ask the bank to keep the consent alive.</summary>
    int ConsentValidDays { get; }

    /// <summary>How far back a sync reaches.</summary>
    int SyncLookbackDays { get; }
}
