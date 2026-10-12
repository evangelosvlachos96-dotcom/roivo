namespace Roivo.Application.Features.Accountant.Services;

/// <summary>
/// How urgently an accountant alert needs attention. Strings rather than an
/// enum to match <c>CashflowAlertSeverity</c>, which the cashflow engine
/// already surfaces to the UI in this shape.
/// </summary>
public static class AccountantAlertSeverity
{
    /// <summary>Money will run out; nothing else on the list outranks it.</summary>
    public const string Critical = "Critical";

    /// <summary>A deadline or a broken integration — actionable today.</summary>
    public const string Warning = "Warning";

    /// <summary>Worth knowing, nothing is breaking.</summary>
    public const string Info = "Info";

    /// <summary>
    /// Sort key, lowest first, so Critical leads the list. Unrecognised values
    /// sort last rather than silently landing among the critical rows.
    /// </summary>
    public static int Rank(string severity) => severity switch
    {
        Critical => 0,
        Warning => 1,
        Info => 2,
        _ => int.MaxValue,
    };
}
