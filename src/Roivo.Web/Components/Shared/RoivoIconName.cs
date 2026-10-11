namespace Roivo.Web.Components.Shared;

/// <summary>
/// The glyphs <see cref="RoivoIcon"/> can draw. An enum rather than a string so
/// a typo is a build error, not a silently empty icon.
/// </summary>
public enum RoivoIconName
{
    None = 0,

    // Navigation
    Overview,
    Cashflow,
    Reconciliation,
    Obligations,
    Reports,
    Connections,
    Help,
    Settings,

    // Portfolio
    Sparkle,
    Plus,
    More,
    Arrow,

    // Chrome
    Business,
    Search,
    Bell,
    Moon,
    Sun,
}
