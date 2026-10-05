using Roivo.Resources;

namespace Roivo.Infrastructure.Email.Templates;

/// <summary>
/// The chrome every Roivo email shares: branded header with the wordmark and
/// gradient rule, a body slot, one gradient CTA button, and a footer carrying
/// the tagline, the notification-settings link and the postal address.
/// </summary>
/// <remarks>
/// <para>
/// Styles are inline on each element rather than in a <c>&lt;style&gt;</c>
/// block because Gmail's web client strips <c>&lt;style&gt;</c> from the body
/// and Outlook ignores most of what survives. Layout is nested tables for the
/// same reason — Outlook's Word rendering engine implements neither flexbox
/// nor grid.
/// </para>
/// <para>
/// That same constraint rules out a <c>@media</c> query for the mobile
/// breakpoint: a media query needs a stylesheet, and the stylesheet is exactly
/// what gets stripped. Responsiveness is built from what every client honours
/// instead — <c>width:100%</c> with <c>max-width:600px</c> on the shell,
/// percentage widths inside the detail tables, and a header whose tagline sits
/// in its own block so it wraps under the wordmark rather than forcing the
/// shell wide. The result is single-column and legible from 320px up without
/// depending on CSS the client may never see.
/// </para>
/// <para>
/// The logo is a monogram cell plus live text, not an image: remote images are
/// blocked by default in Outlook and in Gmail's "ask before displaying" mode,
/// and a base64 PNG would add weight to every single send. Clients that drop
/// the gradient (Outlook) fall back to the solid brand blue declared next to
/// it.
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
    /// <summary>Marks the single call-to-action anchor, for tests and for review.</summary>
    internal const string CtaClass = "roivo-cta";

    /// <summary>Marks the footer's settings anchor so it cannot be mistaken for the CTA.</summary>
    internal const string FooterLinkClass = "roivo-footer-link";

    /// <param name="heading">Plain text; escaped here.</param>
    /// <param name="bodyHtml">Already-escaped markup produced by a template.</param>
    /// <param name="ctaLabel">Plain text; escaped here.</param>
    /// <param name="ctaUrl">Absolute URL the button points at.</param>
    /// <param name="settingsUrl">Absolute URL the footer link points at.</param>
    /// <param name="footnote">Optional small print under the button; plain text.</param>
    /// <param name="notice">
    /// Optional banner above the body, used by the test send to say the figures
    /// below are fabricated. Plain text.
    /// </param>
    /// <param name="footerIntro">
    /// Optional replacement for the default "you have notifications turned on"
    /// line. The account emails are transactional rather than preference-driven,
    /// so claiming otherwise in their footer would be untrue.
    /// </param>
    /// <param name="footerLinkLabel">Optional replacement for the footer link's text.</param>
    /// <param name="postCtaHtml">
    /// Already-escaped markup placed directly under the button. The account
    /// emails put the copy-this-link fallback here, which only makes sense
    /// after the button it is a fallback for.
    /// </param>
    internal static string Render(
        string heading,
        string bodyHtml,
        string ctaLabel,
        string ctaUrl,
        string settingsUrl,
        string? footnote = null,
        string? notice = null,
        string? footerIntro = null,
        string? footerLinkLabel = null,
        string? postCtaHtml = null)
    {
        var footnoteHtml = string.IsNullOrWhiteSpace(footnote)
            ? string.Empty
            : $"""<p style="margin:18px 0 0 0;font-family:{EmailBrand.FontStack};font-size:12px;line-height:1.55;color:{EmailBrand.Muted};">{EmailHtml.Escape(footnote)}</p>""";

        var noticeHtml = string.IsNullOrWhiteSpace(notice)
            ? string.Empty
            : $"""<p style="margin:0 0 20px 0;padding:11px 14px;background-color:#FEF3C7;border-left:3px solid #D97706;font-family:{EmailBrand.FontStack};font-size:13px;line-height:1.5;color:#92400E;">{EmailHtml.Escape(notice)}</p>""";

        var footerText = string.IsNullOrWhiteSpace(footerIntro) ? Notifications.EmailFooter : footerIntro;
        var footerLink = string.IsNullOrWhiteSpace(footerLinkLabel)
            ? Notifications.EmailFooterManage
            : footerLinkLabel;

        return $"""
        <div style="margin:0;padding:0;background-color:{EmailBrand.Light};">
          <table role="presentation" cellpadding="0" cellspacing="0" border="0" width="100%"
                 style="width:100%;border-collapse:collapse;background-color:{EmailBrand.Light};">
            <tr>
              <td align="center" style="padding:24px 12px;background-color:{EmailBrand.Light};">
                <table role="presentation" cellpadding="0" cellspacing="0" border="0" align="center" width="600"
                       style="width:100%;max-width:600px;margin:0 auto;border-collapse:collapse;background-color:{EmailBrand.White};border-radius:12px;overflow:hidden;">
                  <tr>
                    <td style="padding:24px 28px 20px 28px;background-color:{EmailBrand.Navy};">
                      <table role="presentation" cellpadding="0" cellspacing="0" border="0" width="100%"
                             style="width:100%;border-collapse:collapse;">
                        <tr>
                          <td width="44" style="width:44px;padding:0 12px 0 0;vertical-align:middle;">
                            <table role="presentation" cellpadding="0" cellspacing="0" border="0" width="38"
                                   style="width:38px;border-collapse:collapse;background-color:{EmailBrand.Blue};background-image:{EmailBrand.CtaGradient};border-radius:9px;">
                              <tr>
                                <td align="center" height="38"
                                    style="width:38px;height:38px;font-family:{EmailBrand.FontStack};font-size:19px;line-height:38px;font-weight:700;color:{EmailBrand.White};text-align:center;">R</td>
                              </tr>
                            </table>
                          </td>
                          <td style="vertical-align:middle;">
                            <div style="font-family:{EmailBrand.FontStack};font-size:22px;line-height:1.15;font-weight:700;letter-spacing:-0.5px;color:{EmailBrand.White};">{EmailHtml.Escape(Notifications.Wordmark)}</div>
                            <div style="font-family:{EmailBrand.FontStack};font-size:12px;line-height:1.5;color:{EmailBrand.NavyMuted};">{EmailHtml.Escape(Notifications.Tagline)}</div>
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>
                  <tr>
                    <td height="4" style="height:4px;line-height:4px;font-size:0;background-color:{EmailBrand.Blue};background-image:{EmailBrand.BarGradient};">&#8203;</td>
                  </tr>
                  <tr>
                    <td style="padding:30px 28px 28px 28px;">
                      <h1 style="margin:0 0 18px 0;font-family:{EmailBrand.FontStack};font-size:20px;line-height:1.35;font-weight:700;color:{EmailBrand.Ink};">{EmailHtml.Escape(heading)}</h1>
                      {noticeHtml}
                      {bodyHtml}
                      <table role="presentation" cellpadding="0" cellspacing="0" border="0"
                             style="border-collapse:separate;margin:4px 0 0 0;">
                        <tr>
                          <td align="center"
                              style="border-radius:8px;background-color:{EmailBrand.Blue};background-image:{EmailBrand.CtaGradient};">
                            <a class="{CtaClass}" href="{EmailHtml.Escape(ctaUrl)}"
                               style="display:inline-block;padding:13px 26px;font-family:{EmailBrand.FontStack};font-size:15px;line-height:1.2;font-weight:700;color:{EmailBrand.White};text-decoration:none;border-radius:8px;">{EmailHtml.Escape(ctaLabel)}</a>
                          </td>
                        </tr>
                      </table>
                      {postCtaHtml ?? string.Empty}
                      {footnoteHtml}
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:20px 28px 24px 28px;background-color:{EmailBrand.Light};border-top:1px solid {EmailBrand.Line};">
                      <p style="margin:0 0 8px 0;font-family:{EmailBrand.FontStack};font-size:12px;line-height:1.5;font-weight:700;color:{EmailBrand.Navy};">{EmailHtml.Escape(Notifications.Wordmark)} · {EmailHtml.Escape(Notifications.Tagline)}</p>
                      <p style="margin:0 0 8px 0;font-family:{EmailBrand.FontStack};font-size:12px;line-height:1.55;color:{EmailBrand.Muted};">{EmailHtml.Escape(footerText)}</p>
                      <p style="margin:0 0 10px 0;font-family:{EmailBrand.FontStack};font-size:12px;line-height:1.55;">
                        <a class="{FooterLinkClass}" href="{EmailHtml.Escape(settingsUrl)}"
                           style="font-family:{EmailBrand.FontStack};font-size:12px;color:{EmailBrand.Blue};text-decoration:underline;">{EmailHtml.Escape(footerLink)}</a>
                      </p>
                      <p style="margin:0;font-family:{EmailBrand.FontStack};font-size:11px;line-height:1.55;color:{EmailBrand.Muted};">{EmailBrand.PostalAddressPlaceholder}</p>
                    </td>
                  </tr>
                </table>
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
