using Roivo.Resources;

namespace Roivo.Infrastructure.Email.Templates;

/// <summary>
/// Reminder that tax obligations are coming due, one row per obligation.
/// </summary>
public static class TaxReminderEmail
{
    public sealed record Row(string TaxTypeLabel, DateOnly DueDate, decimal Amount);

    /// <param name="DaysUntilDue">Drives the subject line: one day out reads differently from a week out.</param>
    public sealed record Model(
        Guid BusinessId,
        string BusinessName,
        int DaysUntilDue,
        IReadOnlyList<Row> Rows);

    /// <param name="notice">Optional banner text, used by the test send.</param>
    /// <param name="english">
    /// The recipient's language, Greek by default. See <see cref="EmailCulture"/>
    /// for why this is an argument rather than an ambient culture read.
    /// </param>
    public static RenderedEmail Render(
        Model model,
        string baseUrl,
        string? notice = null,
        bool english = false)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);

        using var culture = EmailCulture.For(english);

        var subject = model.DaysUntilDue <= 1
            ? string.Format(EmailHtml.Culture, Notifications.Tax_SubjectTomorrow, model.BusinessName)
            : string.Format(EmailHtml.Culture, Notifications.Tax_Subject, model.DaysUntilDue, model.BusinessName);

        var intro = EmailHtml.Escape(string.Format(
            EmailHtml.Culture, Notifications.Tax_Intro, model.BusinessName, model.Rows.Count));

        var rows = model.Rows.Select(r => EmailHtml.FullWidthRow(
            EmailHtml.Escape(string.Format(
                EmailHtml.Culture,
                Notifications.Tax_RowFormat,
                r.TaxTypeLabel,
                r.DueDate.ToString("dd/MM/yyyy", EmailHtml.Culture),
                r.Amount.ToString("C", EmailHtml.Culture)))));

        var html = EmailLayout.Render(
            heading: Notifications.Tax_Heading,
            bodyHtml: EmailHtml.Paragraph(intro) + EmailHtml.DetailTable(rows),
            ctaLabel: Notifications.Tax_Cta,
            ctaUrl: $"{baseUrl}/businesses/{model.BusinessId}/tax-calendar",
            settingsUrl: EmailLayout.SettingsUrl(baseUrl, model.BusinessId),
            footnote: Notifications.Tax_Footnote,
            notice: notice);

        return new RenderedEmail(subject, html);
    }
}
