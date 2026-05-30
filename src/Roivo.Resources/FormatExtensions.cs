using System.Globalization;

namespace Roivo.Resources;

public static class FormatExtensions
{
    /// <summary>
    /// Shorthand for <see cref="string.Format(IFormatProvider, string, object?[])"/>
    /// using invariant culture. Use with resource strings containing
    /// {0}, {1}, … placeholders.
    /// </summary>
    public static string Format(this string template, params object?[] args)
        => string.Format(CultureInfo.InvariantCulture, template, args);
}
