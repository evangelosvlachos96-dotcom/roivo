namespace Roivo.Application.Features.Accountant.Queries.GetAccountantDashboard;

/// <summary>Outcomes of <see cref="GetAccountantDashboardHandler"/>.</summary>
public abstract record GetAccountantDashboardResult
{
    public sealed record Success(AccountantDashboard Dashboard) : GetAccountantDashboardResult;

    public sealed record Forbidden(string Reason) : GetAccountantDashboardResult;
}
