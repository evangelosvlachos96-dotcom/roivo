namespace Roivo.Web.Components.Shared;

/// <summary>
/// Which line of a cashflow projection to plot. All three come from the
/// forecast engine, which computes a confidence band alongside the expected
/// balance — so switching scenario shows a different real projection rather
/// than the same one scaled.
/// </summary>
public enum ForecastSeries
{
    /// <summary>The engine's central projection.</summary>
    Expected,

    /// <summary>The upper confidence bound.</summary>
    Optimistic,

    /// <summary>The lower confidence bound — the one worth planning against.</summary>
    Conservative,
}
