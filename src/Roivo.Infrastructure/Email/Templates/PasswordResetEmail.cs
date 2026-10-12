namespace Roivo.Infrastructure.Email.Templates;

/// <summary>
/// The password reset email. Like the confirmation, it is a single link whose
/// integrity is the whole product of the message.
/// </summary>
/// <remarks>
/// The reset URL arrives fully built from the forgot-password page model and is
/// escaped for the attribute, nothing more. See
/// <see cref="EmailConfirmationEmail"/> for why that escaping is safe.
/// </remarks>
public static class PasswordResetEmail
{
    /// <param name="ResetUrl">The complete reset URL, email and token included.</param>
    public sealed record Model(string ResetUrl);

    /// <param name="english">The recipient's language, Greek by default.</param>
    public static RenderedEmail Render(Model model, string baseUrl, bool english = false)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(model.ResetUrl);

        using var culture = EmailCulture.For(english);

        var body = EmailHtml.Paragraph(EmailHtml.Escape(AccountEmailCopy.ResetBody(english)));

        var html = EmailLayout.Render(
            heading: AccountEmailCopy.ResetHeading(english),
            bodyHtml: body,
            ctaLabel: AccountEmailCopy.ResetCta(english),
            ctaUrl: model.ResetUrl,
            settingsUrl: $"{baseUrl}/dashboard",
            footnote: AccountEmailCopy.ResetFootnote(english),
            footerIntro: AccountEmailCopy.ResetFooterIntro(english),
            footerLinkLabel: AccountEmailCopy.FooterLinkLabel(english),
            postCtaHtml: EmailHtml.PlainUrlFallback(
                AccountEmailCopy.LinkFallbackLabel(english), model.ResetUrl));

        return new RenderedEmail(AccountEmailCopy.ResetSubject(english), html);
    }
}
