using Roivo.Resources;

namespace Roivo.Infrastructure.Email.Templates;

/// <summary>
/// Notice that an AADE or bank connection has stopped working.
/// </summary>
/// <remarks>
/// The AADE and banking failure jobs predate this template and keep their own
/// plain-text bodies; this is the branded version used by the test send and by
/// any future consolidation of those two jobs.
/// </remarks>
public static class SyncFailureEmail
{
    /// <param name="ConnectionLabel">Which connection broke, already in Greek.</param>
    /// <param name="ReasonText">Why it broke, already in Greek.</param>
    /// <param name="SinceUtc">When the failure streak began.</param>
    /// <param name="CheckPath">Path under the business the CTA opens, e.g. <c>aade</c> or <c>banking</c>.</param>
    public sealed record Model(
        Guid BusinessId,
        string BusinessName,
        string ConnectionLabel,
        string ReasonText,
        DateTime SinceUtc,
        string CheckPath);

    /// <param name="notice">Optional banner text, used by the test send.</param>
    public static RenderedEmail Render(Model model, string baseUrl, string? notice = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);

        var subject = string.Format(EmailHtml.Culture, Notifications.Sync_Subject, model.BusinessName);
        var intro = EmailHtml.Escape(string.Format(
            EmailHtml.Culture, Notifications.Sync_Intro, model.BusinessName));

        var rows = new[]
        {
            EmailHtml.DetailRow(Notifications.Sync_RowConnection, EmailHtml.Escape(model.ConnectionLabel)),
            EmailHtml.DetailRow(Notifications.Sync_RowSince, EmailHtml.DateTimeLocal(model.SinceUtc)),
            EmailHtml.DetailRow(Notifications.Sync_RowReason, EmailHtml.Escape(model.ReasonText)),
        };

        var html = EmailLayout.Render(
            heading: Notifications.Sync_Heading,
            bodyHtml: EmailHtml.Paragraph(intro) + EmailHtml.DetailTable(rows),
            ctaLabel: Notifications.Sync_Cta,
            ctaUrl: $"{baseUrl}/businesses/{model.BusinessId}/{model.CheckPath.Trim('/')}",
            settingsUrl: EmailLayout.SettingsUrl(baseUrl, model.BusinessId),
            notice: notice);

        return new RenderedEmail(subject, html);
    }
}
