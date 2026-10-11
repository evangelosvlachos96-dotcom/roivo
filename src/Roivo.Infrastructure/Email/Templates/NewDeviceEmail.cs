using Roivo.Resources;

namespace Roivo.Infrastructure.Email.Templates;

// BACKLOG: This email template uses inline HTML styling matching current transactional emails.
// When email templates are redesigned in Phase 2 (backlog), this template should be
// updated to match the new platform design system.
// See also: welcome/verification email, password reset email, sync notification emails,
// accountant invitation email — all scheduled for redesign.

/// <summary>
/// Tells an account owner that their credentials were used from a device we
/// have not seen before.
/// </summary>
/// <remarks>
/// This is the one notification that matters when the sign-in was not theirs:
/// whoever signed in sees the in-app banner, the owner sees this. So it names
/// what we know — device, time, network — and nothing we do not.
/// </remarks>
public static class NewDeviceEmail
{
    /// <param name="DeviceName">As the tracker described it, e.g. "Chrome on Windows".</param>
    /// <param name="SignedInAtUtc">When the sign-in happened.</param>
    /// <param name="NetworkPrefix">The network part of the address, never the full one.</param>
    public sealed record Model(string DeviceName, DateTime SignedInAtUtc, string NetworkPrefix);

    /// <param name="english">The recipient's language, Greek by default.</param>
    public static RenderedEmail Render(Model model, string baseUrl, bool english = false)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);

        using var culture = EmailCulture.For(english);

        var body = EmailHtml.Paragraph(EmailHtml.Escape(string.Format(
            EmailHtml.Culture,
            Shell.Device_EmailBody,
            model.DeviceName,
            model.SignedInAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm", EmailHtml.Culture),
            model.NetworkPrefix)));

        var html = EmailLayout.Render(
            heading: Shell.Device_EmailHeading,
            bodyHtml: body,
            ctaLabel: Shell.Device_EmailCta,
            ctaUrl: $"{baseUrl}/settings",
            settingsUrl: $"{baseUrl}/settings",
            footnote: Shell.Device_EmailFootnote);

        return new RenderedEmail(Shell.Device_EmailSubject, html);
    }
}
