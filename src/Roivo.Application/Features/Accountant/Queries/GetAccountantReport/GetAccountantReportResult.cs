namespace Roivo.Application.Features.Accountant.Queries.GetAccountantReport;

/// <summary>Outcomes of <see cref="GetAccountantReportHandler"/>.</summary>
public abstract record GetAccountantReportResult
{
    public sealed record Success(AccountantReport Report) : GetAccountantReportResult;

    public sealed record Forbidden(string Reason) : GetAccountantReportResult;
}
