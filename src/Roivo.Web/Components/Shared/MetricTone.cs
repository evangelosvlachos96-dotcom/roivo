namespace Roivo.Web.Components.Shared;

/// <summary>
/// The tint on a metric card's icon tile. Names match the design's
/// <c>metric-icon--*</c> modifiers, which is what the stylesheet keys on.
/// </summary>
public enum MetricTone
{
    Blue,
    Teal,
    Violet,
    Amber,
}

/// <summary>Whether a metric's note should read as good, bad or neither.</summary>
public enum MetricDirection
{
    Neutral,
    Up,
    Down,
}
