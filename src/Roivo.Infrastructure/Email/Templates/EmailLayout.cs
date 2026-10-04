using Roivo.Resources;

namespace Roivo.Infrastructure.Email.Templates;

/// <summary>
/// The chrome every notification email shares: wordmark header, body slot, CTA
/// button, footer with a link back to the notification settings.
/// </summary>
/// <remarks>
/// <para>
/// Styles are inline on each element rather than in a <c>&lt;style&gt;</c>
/// block because Gmail's web client strips <c>&lt;style&gt;</c> from the body and
/// Outlook ignores most of what survives. Layout is nested tables for the same
/// reason — Outlook's Word rendering engine does not implement flexbox or grid.
/// </para>
/// <para>
/// No templating package: the markup is small, the substitutions are typed, and
/// a dependency that renders strings is a dependency whose escaping rules we
/// would then have to audit. <see cref="EmailHtml.Escape"/> is the single
/// escaping rule instead.
/// </para>
/// </remarks>
internal static class EmailLayout
{
    private const string BrandColor = "#0f766e";

    /// <param name="heading">Plain text; escaped here.</param>
    /// <param name="bodyHtml">Already-escaped markup produced by a template.</param>
    /// <param name="ctaLabel">Plain text; escaped here.</param>
    /// <param name="ctaUrl">Absolute URL the button points at.</param>
    /// <param name="settingsUrl">Absolute URL of the recipient's notification settings.</param>
    /// <param name="footnote">Optional small print under the button; plain text.</param>
    /// <param name="notice">
    /// Optional banner above the body, used by the test send to say the figures
    /// below are fabricated. Plain text.
    /// </param>
    internal static string Render(
        string heading,
        string bodyHtml,
        string ctaLabel,
        string ctaUrl,
        string settingsUrl,
        string? footnote = null,
        string? notice = null)
    {
        var footnoteHtml = string.IsNullOrWhiteSpace(footnote)
            ? string.Empty
            : $"""<p style="margin:16px 0 0 0;font-size:12px;line-height:1.5;color:#6b7280;">{EmailHtml.Escape(footnote)}</p>""";

        var noticeHtml = string.IsNullOrWhiteSpace(notice)
            ? string.Empty
            : $"""<p style="margin:0 0 18px 0;padding:10px 14px;background-color:#fef3c7;border-left:3px solid #d97706;font-size:13px;line-height:1.5;color:#92400e;">{EmailHtml.Escape(notice)}</p>""";

        return $"""
        <div style="margin:0;padding:24px 12px;background-color:#f3f4f6;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
          <table role="presentation" cellpadding="0" cellspacing="0" border="0" align="center" width="600"
                 style="width:100%;max-width:600px;margin:0 auto;border-collapse:collapse;background-color:#ffffff;border-radius:10px;overflow:hidden;">
            <tr>
              <td style="padding:22px 28px;background-color:{BrandColor};">
                <span style="font-size:22px;font-weight:700;letter-spacing:-0.5px;color:#ffffff;">{EmailHtml.Escape(Notifications.Wordmark)}</span>
                <span style="font-size:12px;color:#ccfbf1;padding-left:10px;">{EmailHtml.Escape(Notifications.Tagline)}</span>
              </td>
            </tr>
            <tr>
              <td style="padding:28px;">
                <h1 style="margin:0 0 16px 0;font-size:19px;line-height:1.35;color:#111827;font-weight:700;">{EmailHtml.Escape(heading)}</h1>
                {noticeHtml}
                {bodyHtml}
                <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="border-collapse:collapse;margin:8px 0 0 0;">
                  <tr>
                    <td style="border-radius:6px;background-color:{BrandColor};">
                      <a href="{EmailHtml.Escape(ctaUrl)}"
                         style="display:inline-block;padding:12px 22px;font-size:15px;font-weight:600;color:#ffffff;text-decoration:none;border-radius:6px;">{EmailHtml.Escape(ctaLabel)}</a>
                    </td>
                  </tr>
                </table>
                {footnoteHtml}
              </td>
            </tr>
            <tr>
              <td style="padding:18px 28px;background-color:#f9fafb;border-top:1px solid #e5e7eb;">
                <p style="margin:0 0 6px 0;font-size:12px;line-height:1.5;color:#6b7280;">{EmailHtml.Escape(Notifications.EmailFooter)}</p>
                <a href="{EmailHtml.Escape(settingsUrl)}" style="font-size:12px;color:{BrandColor};text-decoration:underline;">{EmailHtml.Escape(Notifications.EmailFooterManage)}</a>
              </td>
            </tr>
          </table>
        </div>
        """;
    }

    /// <summary>The settings page a footer link points at, for one business.</summary>
    internal static string SettingsUrl(string baseUrl, Guid businessId)
        => $"{baseUrl}/businesses/{businessId}/settings/notifications";
}
