namespace Roivo.Infrastructure.Email.Templates;

/// <summary>A notification email ready to hand to <see cref="IEmailSender"/>.</summary>
/// <param name="Subject">Plain text; mail clients render it unescaped.</param>
/// <param name="HtmlBody">Complete, self-contained HTML with inline styles.</param>
public sealed record RenderedEmail(string Subject, string HtmlBody);
