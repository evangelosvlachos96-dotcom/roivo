namespace Roivo.Application.Features.Accountant.Queries.GetAccountantAlerts;

/// <summary>Outcomes of <see cref="GetAccountantAlertsHandler"/>.</summary>
public abstract record GetAccountantAlertsResult
{
    /// <summary>Alerts ordered Critical → Warning → Info.</summary>
    public sealed record Success(IReadOnlyList<AccountantAlert> Alerts) : GetAccountantAlertsResult;

    public sealed record Forbidden(string Reason) : GetAccountantAlertsResult;
}
