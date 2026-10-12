using Roivo.Resources;

namespace Roivo.Infrastructure.Email.Templates;

/// <summary>
/// Warning that the stored forecast dips to or below the recipient's threshold.
/// </summary>
public static class CashflowAlertEmail
{
    /// <param name="LowestBalance">The worst projected balance in the horizon.</param>
    /// <param name="LowestDate">The day that balance falls on.</param>
    /// <param name="Threshold">The recipient's own trigger level, echoed so the alert explains itself.</param>
    public sealed record Model(
        Guid BusinessId,
        string BusinessName,
        decimal LowestBalance,
        DateOnly LowestDate,
        decimal Threshold);

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

        var subject = string.Format(EmailHtml.Culture, Notifications.Cashflow_Subject, model.BusinessName);

        var intro = EmailHtml.Escape(string.Format(
            EmailHtml.Culture,
            Notifications.Cashflow_Intro,
            model.BusinessName,
            model.LowestBalance.ToString("C", EmailHtml.Culture),
            model.LowestDate.ToString("dd/MM/yyyy", EmailHtml.Culture)));

        var rows = new[]
        {
            EmailHtml.DetailRow(Notifications.Cashflow_RowLowestBalance, EmailHtml.Money(model.LowestBalance)),
            EmailHtml.DetailRow(Notifications.Cashflow_RowLowestDate, EmailHtml.Date(model.LowestDate)),
            EmailHtml.DetailRow(Notifications.Cashflow_RowThreshold, EmailHtml.Money(model.Threshold)),
        };

        var html = EmailLayout.Render(
            heading: Notifications.Cashflow_Heading,
            bodyHtml: EmailHtml.Paragraph(intro) + EmailHtml.DetailTable(rows),
            ctaLabel: Notifications.Cashflow_Cta,
            ctaUrl: $"{baseUrl}/businesses/{model.BusinessId}/cashflow",
            settingsUrl: EmailLayout.SettingsUrl(baseUrl, model.BusinessId),
            footnote: Notifications.Cashflow_Footnote,
            notice: notice);

        return new RenderedEmail(subject, html);
    }
}
