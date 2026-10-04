using Roivo.Resources;

namespace Roivo.Infrastructure.Email.Templates;

/// <summary>
/// The morning digest: one line per business the recipient watches.
/// </summary>
public static class DailyDigestEmail
{
    /// <param name="BusinessId">Only the first row's id is used for the CTA; a digest spans several businesses.</param>
    public sealed record Row(
        Guid BusinessId,
        string BusinessName,
        int NewInvoices,
        int NewTransactions,
        int Unmatched);

    public sealed record Model(DateOnly Date, IReadOnlyList<Row> Rows);

    /// <param name="notice">Optional banner text, used by the test send.</param>
    public static RenderedEmail Render(Model model, string baseUrl, string? notice = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);

        var dateText = model.Date.ToString("dd/MM/yyyy", EmailHtml.Culture);
        var subject = string.Format(EmailHtml.Culture, Notifications.Digest_Subject, dateText);

        var intro = model.Rows.Count == 0
            ? EmailHtml.Escape(Notifications.Digest_IntroEmpty)
            : EmailHtml.Escape(string.Format(
                EmailHtml.Culture, Notifications.Digest_Intro, model.Rows.Count, dateText));

        var rows = model.Rows.Select(r => EmailHtml.DetailRow(
            r.BusinessName,
            EmailHtml.Escape(string.Format(
                EmailHtml.Culture,
                Notifications.Digest_RowFormat,
                r.NewInvoices,
                r.NewTransactions,
                r.Unmatched))));

        var body = EmailHtml.Paragraph(intro)
            + (model.Rows.Count == 0 ? string.Empty : EmailHtml.DetailTable(rows));

        // The digest covers a portfolio, so the button goes to the dashboard
        // rather than to any one business.
        var html = EmailLayout.Render(
            heading: Notifications.Digest_Heading,
            bodyHtml: body,
            ctaLabel: Notifications.Digest_Cta,
            ctaUrl: $"{baseUrl}/dashboard",
            settingsUrl: SettingsUrlFor(model, baseUrl),
            notice: notice);

        return new RenderedEmail(subject, html);
    }

    private static string SettingsUrlFor(Model model, string baseUrl)
        => model.Rows.Count > 0
            ? EmailLayout.SettingsUrl(baseUrl, model.Rows[0].BusinessId)
            : $"{baseUrl}/dashboard";
}
