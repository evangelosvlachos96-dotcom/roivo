namespace Roivo.Application.Features.Reconciliation.Commands.ManualMatch;

public sealed record ManualMatchCommand(
    Guid BusinessId,
    Guid InvoiceId,
    Guid BankTransactionId,
    string UserId,
    string? Notes = null);
