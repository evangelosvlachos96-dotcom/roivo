using System.Globalization;
using System.Net;

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
    /// <summary>Formatting culture for money and dates. Recipients are Greek businesses.</summary>
    internal static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("el-GR");

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
    internal static string DetailRow(string label, string escapedValue) =>
        $"""
        <tr>
          <td style="padding:6px 0;font-size:14px;color:#5b6472;">{Escape(label)}</td>
          <td style="padding:6px 0;font-size:14px;color:#111827;font-weight:600;text-align:right;">{escapedValue}</td>
        </tr>
        """;

    /// <summary>
    /// A row whose content spans both columns, for list lines that read as one
    /// sentence rather than as a label and a value.
    /// </summary>
    internal static string FullWidthRow(string escapedValue) =>
        $"""
        <tr>
          <td colspan="2" style="padding:6px 0;font-size:14px;line-height:1.5;color:#111827;">{escapedValue}</td>
        </tr>
        """;

    /// <summary>A paragraph of body copy.</summary>
    internal static string Paragraph(string escapedText) =>
        $"""<p style="margin:0 0 14px 0;font-size:15px;line-height:1.55;color:#374151;">{escapedText}</p>""";

    /// <summary>Wraps detail rows in the bordered table the templates share.</summary>
    internal static string DetailTable(IEnumerable<string> rows) =>
        $"""
        <table role="presentation" cellpadding="0" cellspacing="0" border="0" width="100%"
               style="width:100%;border-collapse:collapse;border-top:1px solid #e5e7eb;border-bottom:1px solid #e5e7eb;margin:0 0 20px 0;">
          {string.Concat(rows)}
        </table>
        """;
}
