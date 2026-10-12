namespace Roivo.Infrastructure.Email.Templates;

/// <summary>
/// The Roivo brand palette and type stack, as literals usable inside an inline
/// <c>style</c> attribute.
/// </summary>
/// <remarks>
/// <para>
/// Mail clients get no stylesheet from us (see <see cref="EmailLayout"/>), so
/// these cannot be CSS custom properties — every value is pasted into a style
/// attribute at render time. Keeping them here means a palette change is one
/// edit rather than a search across seven templates.
/// </para>
/// <para>
/// <see cref="FontStack"/> names Inter first even though almost no mail client
/// will have it: Apple Mail and a handful of desktop clients do pick up a
/// locally installed Inter, and the Arial/Helvetica fallback is what everyone
/// else renders. Web fonts are deliberately not loaded — <c>@font-face</c>
/// needs a stylesheet, which Gmail strips.
/// </para>
/// </remarks>
internal static class EmailBrand
{
    internal const string Navy = "#0B1F3A";
    internal const string Blue = "#1E88E5";
    internal const string Teal = "#2DD4BF";
    internal const string Light = "#F8FAFC";

    internal const string Ink = "#0F172A";
    internal const string Body = "#334155";
    internal const string Muted = "#64748B";
    internal const string Line = "#E2E8F0";
    internal const string White = "#FFFFFF";
    internal const string NavyMuted = "#94A3B8";

    /// <summary>Linear gradient used by the CTA button and the header rule.</summary>
    internal const string CtaGradient = "linear-gradient(135deg, #1E88E5, #2DD4BF)";

    internal const string BarGradient = "linear-gradient(90deg, #1E88E5, #2DD4BF)";

    internal const string FontStack = "Inter,'Helvetica Neue',Helvetica,Arial,sans-serif";

    /// <summary>
    /// Physical postal address required by CAN-SPAM and expected under GDPR's
    /// identification duty. Left as a placeholder on purpose: inventing an
    /// address would be worse than shipping an obvious one to fill in.
    /// </summary>
    internal const string PostalAddressPlaceholder = "[COMPANY_ADDRESS]";
}
