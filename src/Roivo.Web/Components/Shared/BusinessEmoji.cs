namespace Roivo.Web.Components.Shared;

/// <summary>
/// A glyph standing in for a business in the sidebar switcher.
/// </summary>
/// <remarks>
/// <para>
/// Chosen from the ΚΑΔ rather than the name. ΚΑΔ is the activity code every
/// Greek business is registered under, so this classifies a real customer as
/// well as it classifies the seed — matching on words in the name would only
/// ever work for the businesses we happened to think of.
/// </para>
/// <para>
/// The ranges are the NACE sections ΚΑΔ is built on, which is why two digits
/// are enough: 62 is any software activity, 41 any building construction.
/// </para>
/// </remarks>
public static class BusinessEmoji
{
    /// <summary>The accountant's own practice, which has no ΚΑΔ of its own here.</summary>
    public const string Practice = "\U0001F3DB️";   // 🏛️

    private const string Fallback = "\U0001F3E2";        // 🏢

    public static string For(string? kad)
    {
        if (string.IsNullOrWhiteSpace(kad)) return Fallback;

        var digits = kad.Trim();
        if (digits.Length < 2 || !int.TryParse(digits[..2], out var section))
            return Fallback;

        return section switch
        {
            >= 1 and <= 3 => "\U0001F33E",               // 🌾 agriculture, forestry, fishing
            >= 5 and <= 9 => "⛏️",             // ⛏️ mining
            >= 10 and <= 33 => "\U0001F3ED",             // 🏭 manufacturing
            >= 35 and <= 39 => "⚡",                 // ⚡ energy, water, waste
            >= 41 and <= 43 => "\U0001F3D7️",       // 🏗️ construction
            >= 45 and <= 47 => "\U0001F3EA",             // 🏪 wholesale and retail trade
            >= 49 and <= 53 => "\U0001F69B",             // 🚛 transport and storage
            55 => "\U0001F3E8",                          // 🏨 accommodation
            56 => "\U0001F37D️",                    // 🍽️ food and drink service
            >= 58 and <= 63 => "\U0001F4BB",             // 💻 information and communication
            >= 64 and <= 66 => "\U0001F3E6",             // 🏦 finance and insurance
            68 => "\U0001F3E0",                          // 🏠 real estate
            >= 69 and <= 75 => "\U0001F4BC",             // 💼 professional, scientific, technical
            >= 77 and <= 82 => "\U0001F4CB",             // 📋 administrative and support
            >= 85 and <= 85 => "\U0001F393",             // 🎓 education
            >= 86 and <= 88 => "\U0001F3E5",             // 🏥 health and social work
            >= 90 and <= 93 => "\U0001F3AD",             // 🎭 arts, entertainment, recreation
            _ => Fallback,
        };
    }
}
