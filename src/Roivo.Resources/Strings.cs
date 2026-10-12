using System.Globalization;

namespace Roivo.Resources;

/// <summary>
/// Culture-aware lookup behind every user-facing string.
/// </summary>
/// <remarks>
/// The resource classes used to expose <c>public const string</c>. A const is
/// inlined at compile time, so it can never vary by culture — the whole product
/// was Greek by construction. They are now <c>static string</c> properties that
/// call <see cref="Get"/>, which keeps all ~560 existing call sites
/// (<c>@Common.SaveButton</c>) working untouched while making them bilingual.
///
/// Greek is the fallback rather than English: this is a product for Greek
/// businesses, and a missing translation should degrade to the language the
/// users actually read.
/// </remarks>
public static class Strings
{
    /// <summary>The product's primary culture.</summary>
    public const string Greek = "el";

    /// <summary>The secondary culture.</summary>
    public const string English = "en";

    /// <summary>
    /// English overrides, keyed by the same identifier as the Greek property.
    /// Anything absent falls back to Greek.
    /// </summary>
    private static readonly Dictionary<string, string> EnglishByKey =
        new(StringComparer.Ordinal);

    /// <summary>
    /// Registers the English text for a batch of keys. Called once per resource
    /// class from its static initialiser.
    /// </summary>
    public static void RegisterEnglish(IReadOnlyDictionary<string, string> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        foreach (var (key, value) in entries)
            EnglishByKey[key] = value;
    }

    /// <summary>
    /// The string for the current UI culture. <paramref name="greek"/> is the
    /// literal from the resource class, so Greek costs no lookup at all.
    /// </summary>
    /// <param name="key">
    /// Stable identifier, conventionally <c>ClassName.MemberName</c>. Supplied
    /// explicitly rather than derived, so renaming a property cannot silently
    /// orphan its translation.
    /// </param>
    public static string Get(string key, string greek)
        => IsEnglish && EnglishByKey.TryGetValue(key, out var english)
            ? english
            : greek;

    /// <summary>True when the current UI culture is English.</summary>
    public static bool IsEnglish =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
            .Equals(English, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Culture used for formatting money and dates. Greek and English both get
    /// <c>el-GR</c> conventions for dates — a Greek business reads 04/10/2026
    /// whichever language the interface is in — but English gets its own
    /// number/currency grouping.
    /// </summary>
    public static CultureInfo FormatCulture =>
        IsEnglish ? CultureInfo.GetCultureInfo("en-IE") : CultureInfo.GetCultureInfo("el-GR");

    /// <summary>How many English translations are registered. Diagnostics only.</summary>
    public static int TranslationCount => EnglishByKey.Count;
}
