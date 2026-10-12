namespace Roivo.Web.Configuration.Settings;

/// <summary>
/// Who may open the Hangfire dashboard outside Development.
/// </summary>
/// <remarks>
/// There is no admin role to key off: sign-in produces <c>tenant_id</c> and
/// <c>tenant_type</c> (Accountant/Business) and nothing seeds an elevated role.
/// An explicit allowlist keeps the decision in configuration, where staging can
/// set it without a deployment, and fails closed when it is empty.
/// </remarks>
public class HangfireDashboardSettings
{
    /// <summary>
    /// Comma- or semicolon-separated email addresses allowed to open the
    /// dashboard, compared case-insensitively against the signed-in user's
    /// email claim. Empty denies everyone — the dashboard exposes job arguments
    /// and stack traces for every tenant, so it must never default to open.
    /// </summary>
    /// <remarks>
    /// A single delimited string rather than a list: this is set as one
    /// environment variable on Render, and binding a list from the environment
    /// would need indexed keys (<c>...__0</c>, <c>...__1</c>).
    /// </remarks>
    public string AuthorizedEmails { get; init; } = string.Empty;

    /// <summary>Parses <see cref="AuthorizedEmails"/> into a lookup set.</summary>
    public IReadOnlySet<string> ParseAuthorizedEmails() =>
        AuthorizedEmails
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
}
