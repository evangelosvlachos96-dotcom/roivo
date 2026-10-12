namespace Roivo.Infrastructure.Email.Templates;

/// <summary>
/// The registration confirmation email: the one message a new account cannot
/// get past.
/// </summary>
/// <remarks>
/// The CTA URL is handed in fully built, token and all, by the registration
/// page model — this template never touches it beyond HTML-escaping it for the
/// <c>href</c> attribute. Identity tokens are query-escaped already, so the
/// only character escaping can touch is the <c>&amp;</c> between query
/// parameters, which every client and browser decodes back before navigating.
/// Re-encoding or trimming the URL here would break sign-up outright.
/// </remarks>
public static class EmailConfirmationEmail
{
    /// <param name="FullName">As typed at registration; escaped at render.</param>
    /// <param name="ConfirmUrl">The complete confirmation URL, token included.</param>
    public sealed record Model(string FullName, string ConfirmUrl);

    /// <param name="english">The recipient's language, Greek by default.</param>
    public static RenderedEmail Render(Model model, string baseUrl, bool english = false)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(model.ConfirmUrl);

        using var culture = EmailCulture.For(english);

        var greeting = EmailHtml.Escape(string.Format(
            EmailHtml.Culture, AccountEmailCopy.ConfirmGreeting(english), model.FullName));

        var body = EmailHtml.Paragraph(greeting)
            + EmailHtml.Paragraph(EmailHtml.Escape(AccountEmailCopy.ConfirmBody(english)));

        var html = EmailLayout.Render(
            heading: AccountEmailCopy.ConfirmHeading(english),
            bodyHtml: body,
            ctaLabel: AccountEmailCopy.ConfirmCta(english),
            ctaUrl: model.ConfirmUrl,
            settingsUrl: $"{baseUrl}/dashboard",
            footnote: AccountEmailCopy.ConfirmFootnote(english),
            footerIntro: AccountEmailCopy.ConfirmFooterIntro(english),
            footerLinkLabel: AccountEmailCopy.FooterLinkLabel(english),
            postCtaHtml: EmailHtml.PlainUrlFallback(
                AccountEmailCopy.LinkFallbackLabel(english), model.ConfirmUrl));

        return new RenderedEmail(AccountEmailCopy.ConfirmSubject(english), html);
    }
}
