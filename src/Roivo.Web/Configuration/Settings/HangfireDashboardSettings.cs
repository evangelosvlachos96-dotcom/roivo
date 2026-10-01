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
    /// Email addresses allowed to open the dashboard. Compared
    /// case-insensitively against the signed-in user's email claim. Empty
    /// denies everyone — the dashboard exposes job arguments and stack traces
    /// for every tenant, so it must never default to open.
    /// </summary>
    public IReadOnlyList<string> AuthorizedEmails { get; init; } = [];
}
