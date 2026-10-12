using MudBlazor;

namespace Roivo.Web.Configuration;

/// <summary>
/// The Roivo brand as a MudBlazor theme, so every component picks the palette up
/// instead of each page restating hex codes.
/// </summary>
public static class RoivoTheme
{
    public const string Navy = "#0B1F3A";
    public const string Blue = "#1E88E5";
    public const string Teal = "#2DD4BF";
    public const string Light = "#F8FAFC";

    /// <summary>Hero and call-to-action gradient.</summary>
    public const string Gradient = "linear-gradient(135deg, #1E88E5, #2DD4BF)";

    public static MudTheme Build() => new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = Blue,
            Secondary = Teal,
            Tertiary = Navy,
            AppbarBackground = Navy,
            AppbarText = Light,
            Background = Light,
            Surface = "#FFFFFF",
            TextPrimary = Navy,
            // Deliberately not the brand teal: success needs to stay
            // distinguishable from the accent colour used on neutral chrome.
            Success = "#16A34A",
            Warning = "#D97706",
            Error = "#DC2626",
            Info = Blue,
        },

        Typography = new Typography
        {
            Default = new DefaultTypography { FontFamily = FontStack, FontWeight = "400" },
            H1 = new H1Typography { FontFamily = FontStack, FontWeight = "700" },
            H2 = new H2Typography { FontFamily = FontStack, FontWeight = "700" },
            H3 = new H3Typography { FontFamily = FontStack, FontWeight = "600" },
            H4 = new H4Typography { FontFamily = FontStack, FontWeight = "600" },
            H5 = new H5Typography { FontFamily = FontStack, FontWeight = "600" },
            H6 = new H6Typography { FontFamily = FontStack, FontWeight = "600" },
            Button = new ButtonTypography { FontFamily = FontStack, FontWeight = "600", TextTransform = "none" },
            Body1 = new Body1Typography { FontFamily = FontStack },
            Body2 = new Body2Typography { FontFamily = FontStack },
            Caption = new CaptionTypography { FontFamily = FontStack },
            Subtitle1 = new Subtitle1Typography { FontFamily = FontStack, FontWeight = "500" },
            Subtitle2 = new Subtitle2Typography { FontFamily = FontStack, FontWeight = "500" },
        },

        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "10px",
        },
    };

    /// <summary>Inter, with native fallbacks for the moment before the webfont lands.</summary>
    private static readonly string[] FontStack =
        ["Inter", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "Roboto", "Helvetica Neue", "Arial", "sans-serif"];
}
