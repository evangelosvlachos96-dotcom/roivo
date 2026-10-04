using Roivo.Resources;

namespace Roivo.Infrastructure.Email.Templates;

/// <summary>
/// Weekly summary of how much of the week's activity reconciled itself and
/// what is still waiting for a human.
/// </summary>
public static class WeeklyReconciliationEmail
{
    /// <param name="MatchRate">A fraction in 0..1, not a percentage.</param>
    public sealed record Model(
        Guid BusinessId,
        string BusinessName,
        DateOnly From,
        DateOnly To,
        decimal MatchRate,
        int ConfirmedMatches,
        int PendingMatches,
        int UnmatchedInvoices,
        int UnmatchedTransactions);

    /// <param name="notice">Optional banner text, used by the test send.</param>
    public static RenderedEmail Render(Model model, string baseUrl, string? notice = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);

        var subject = string.Format(
            EmailHtml.Culture, Notifications.Reconciliation_Subject, model.BusinessName);

        var intro = EmailHtml.Escape(string.Format(
            EmailHtml.Culture,
            Notifications.Reconciliation_Intro,
            model.BusinessName,
            model.From.ToString("dd/MM/yyyy", EmailHtml.Culture),
            model.To.ToString("dd/MM/yyyy", EmailHtml.Culture)));

        var rows = new[]
        {
            EmailHtml.DetailRow(Notifications.Reconciliation_RowMatchRate, EmailHtml.Percent(model.MatchRate)),
            EmailHtml.DetailRow(Notifications.Reconciliation_RowConfirmed, EmailHtml.Number(model.ConfirmedMatches)),
            EmailHtml.DetailRow(Notifications.Reconciliation_RowPending, EmailHtml.Number(model.PendingMatches)),
            EmailHtml.DetailRow(Notifications.Reconciliation_RowUnmatchedInvoices, EmailHtml.Number(model.UnmatchedInvoices)),
            EmailHtml.DetailRow(Notifications.Reconciliation_RowUnmatchedTransactions, EmailHtml.Number(model.UnmatchedTransactions)),
        };

        var html = EmailLayout.Render(
            heading: Notifications.Reconciliation_Heading,
            bodyHtml: EmailHtml.Paragraph(intro) + EmailHtml.DetailTable(rows),
            ctaLabel: Notifications.Reconciliation_Cta,
            ctaUrl: $"{baseUrl}/businesses/{model.BusinessId}/reconciliation",
            settingsUrl: EmailLayout.SettingsUrl(baseUrl, model.BusinessId),
            notice: notice);

        return new RenderedEmail(subject, html);
    }
}
