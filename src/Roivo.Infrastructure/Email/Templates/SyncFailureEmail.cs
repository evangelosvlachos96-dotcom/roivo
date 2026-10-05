using Roivo.Resources;

namespace Roivo.Infrastructure.Email.Templates;

/// <summary>
/// Notice that an AADE or bank connection has stopped working.
/// </summary>
/// <remarks>
/// Shared by the AADE job, the banking job and the test send, so the one
/// branded layout covers every "a connection broke" message.
/// </remarks>
public static class SyncFailureEmail
{
    /// <param name="ConnectionLabel">Which connection broke, already in Greek.</param>
    /// <param name="ReasonText">Why it broke, already in Greek.</param>
    /// <param name="SinceUtc">When the failure streak began.</param>
    /// <param name="CheckPath">Path under the business the CTA opens, e.g. <c>aade</c> or <c>banking</c>.</param>
    /// <param name="FailedAttempts">
    /// Consecutive failures, when the caller counts them. Banking does; AADE
    /// records only the first failure time, so it leaves this null and the row
    /// is omitted rather than rendered as a misleading zero.
    /// </param>
    public sealed record Model(
        Guid BusinessId,
        string BusinessName,
        string ConnectionLabel,
        string ReasonText,
        DateTime SinceUtc,
        string CheckPath,
        int? FailedAttempts = null);

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

        var subject = string.Format(EmailHtml.Culture, Notifications.Sync_Subject, model.BusinessName);
        var intro = EmailHtml.Escape(string.Format(
            EmailHtml.Culture, Notifications.Sync_Intro, model.BusinessName));

        var rows = new List<string>
        {
            EmailHtml.DetailRow(Notifications.Sync_RowConnection, EmailHtml.Escape(model.ConnectionLabel)),
            EmailHtml.DetailRow(Notifications.Sync_RowSince, EmailHtml.DateTimeLocal(model.SinceUtc)),
        };

        if (model.FailedAttempts is int attempts)
        {
            rows.Add(EmailHtml.DetailRow(
                Notifications.Sync_RowAttempts,
                attempts.ToString(EmailHtml.Culture)));
        }

        rows.Add(EmailHtml.DetailRow(Notifications.Sync_RowReason, EmailHtml.Escape(model.ReasonText)));

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
