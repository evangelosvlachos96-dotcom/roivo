namespace Roivo.Application.Features.Cashflow.Commands.MarkTaxPaid;

/// <summary>Outcomes of <see cref="MarkTaxPaidHandler"/>.</summary>
public abstract record MarkTaxPaidResult
{
    public sealed record Success : MarkTaxPaidResult;
    public sealed record Forbidden(string Reason) : MarkTaxPaidResult;
    public sealed record NotFound : MarkTaxPaidResult;
    public sealed record AlreadyPaid : MarkTaxPaidResult;
    public sealed record InvalidAmount : MarkTaxPaidResult;
}
