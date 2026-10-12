using System.Globalization;
using System.Net;
using Roivo.Resources;

namespace Roivo.Infrastructure.Email.Templates;

/// <summary>
/// Escaping and formatting primitives shared by the notification templates.
/// </summary>
/// <remarks>
/// Every template builds its HTML by interpolation, so escaping is not
/// optional: counterparty and business names come from AADE and from bank
/// statement narratives, both of which happily contain <c>&amp;</c> and
/// <c>&lt;</c>. An unescaped <c>&lt;</c> in a trade name does not merely look
/// wrong — it swallows the rest of the element and breaks the layout for
/// everyone downstream of it. Call <see cref="Escape"/> on every interpolated
/// value without exception.
/// </remarks>
internal static class EmailHtml
{
    /// <summary>
    /// Formatting culture for money and dates, following the language the
    /// current render is scoped to by <see cref="EmailCulture"/>.
    /// </summary>
    /// <remarks>
    /// This used to be a fixed <c>el-GR</c> field. It has to vary now: an
    /// English recipient reading "1.240,55 €" would misread the amount by three
    /// orders of magnitude. Dates stay <c>dd/MM/yyyy</c> in both languages —
    /// a Greek business reads day-first whichever language the product is in —
    /// which is why <see cref="Date"/> passes an explicit pattern rather than
    /// letting the culture choose.
    /// </remarks>
    internal static CultureInfo Culture => Strings.FormatCulture;

    /// <summary>HTML-escapes a value for use in element content or a quoted attribute.</summary>
    internal static string Escape(string? value)
        => WebUtility.HtmlEncode(value ?? string.Empty);

    internal static string Money(decimal value)
        => Escape(value.ToString("C", Culture));

    internal static string Date(DateOnly value)
        => Escape(value.ToString("dd/MM/yyyy", Culture));

    internal static string DateTimeLocal(DateTime utc)
        => Escape(utc.ToLocalTime().ToString("dd/MM/yyyy HH:mm", Culture));

    internal static string Number(int value)
        => Escape(value.ToString("N0", Culture));

    internal static string Percent(decimal fraction)
        => Escape(fraction.ToString("P1", Culture));

    /// <summary>
    /// Trims a configured base URL to a form safe to concatenate a path onto,
    /// falling back to production when configuration is missing.
    /// </summary>
    internal static string NormalizeBaseUrl(string? configured)
        => string.IsNullOrWhiteSpace(configured) ? "https://roivo.gr" : configured.TrimEnd('/');

    /// <summary>A label/value line, as used in every template's detail block.</summary>
    /// <remarks>
    /// Percentage widths rather than fixed pixels, so the pair keeps its
    /// proportions on a 320px phone without depending on a media query.
    /// </remarks>
    internal static string DetailRow(string label, string escapedValue) =>
        $"""
        <tr>
          <td width="55%" style="width:55%;padding:9px 12px 9px 0;font-family:{EmailBrand.FontStack};font-size:14px;line-height:1.45;color:{EmailBrand.Muted};border-bottom:1px solid {EmailBrand.Line};">{Escape(label)}</td>
          <td width="45%" style="width:45%;padding:9px 0;font-family:{EmailBrand.FontStack};font-size:14px;line-height:1.45;color:{EmailBrand.Ink};font-weight:700;text-align:right;border-bottom:1px solid {EmailBrand.Line};">{escapedValue}</td>
        </tr>
        """;

    /// <summary>
    /// A row whose content spans both columns, for list lines that read as one
    /// sentence rather than as a label and a value.
    /// </summary>
    internal static string FullWidthRow(string escapedValue) =>
        $"""
        <tr>
          <td colspan="2" style="padding:9px 0;font-family:{EmailBrand.FontStack};font-size:14px;line-height:1.5;color:{EmailBrand.Ink};border-bottom:1px solid {EmailBrand.Line};">{escapedValue}</td>
        </tr>
        """;

    /// <summary>A paragraph of body copy.</summary>
    internal static string Paragraph(string escapedText) =>
        $"""<p style="margin:0 0 16px 0;font-family:{EmailBrand.FontStack};font-size:15px;line-height:1.6;color:{EmailBrand.Body};">{escapedText}</p>""";

    /// <summary>
    /// The copy-me-instead fallback under a CTA, for clients that mangle the
    /// button and for users forwarding the mail to a different device.
    /// </summary>
    /// <remarks>
    /// Deliberately not an anchor. The CTA must be the only clickable link in
    /// the message body so that link-tracking rewrites and "exactly one CTA"
    /// cannot disagree about which URL the recipient is meant to follow.
    /// </remarks>
    internal static string PlainUrlFallback(string label, string url) =>
        $"""
        <p style="margin:18px 0 0 0;font-family:{EmailBrand.FontStack};font-size:12px;line-height:1.55;color:{EmailBrand.Muted};">{Escape(label)}</p>
        <p style="margin:6px 0 0 0;font-family:{EmailBrand.FontStack};font-size:12px;line-height:1.6;color:{EmailBrand.Blue};word-break:break-all;">{Escape(url)}</p>
        """;

    /// <summary>Wraps detail rows in the bordered table the templates share.</summary>
    internal static string DetailTable(IEnumerable<string> rows) =>
        $"""
        <table role="presentation" cellpadding="0" cellspacing="0" border="0" width="100%"
               style="width:100%;border-collapse:collapse;border-top:1px solid {EmailBrand.Line};margin:0 0 20px 0;">
          {string.Concat(rows)}
        </table>
        """;
}
