namespace Roivo.Application.Features.Cashflow.Commands.MarkTaxPaid;

public sealed record MarkTaxPaidCommand(Guid TaxObligationId, decimal ActualAmount, DateTime PaidAtUtc);
