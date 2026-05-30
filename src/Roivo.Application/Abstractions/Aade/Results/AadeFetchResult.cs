namespace Roivo.Application.Abstractions.Aade.Results;

/// <summary>Outcome of an AADE invoice-fetch call.</summary>
public abstract record AadeFetchResult
{
    public sealed record Success(
        IReadOnlyList<AadeInvoiceDto> Incoming,
        IReadOnlyList<AadeBookEntryDto> Outgoing,
        long MaxIncomingMark,
        long MaxOutgoingMark) : AadeFetchResult;
    public sealed record InvalidCredentials : AadeFetchResult;
    public sealed record NetworkError(string Message) : AadeFetchResult;
    public sealed record AadeServerError(int StatusCode, string Message) : AadeFetchResult;
}

/// <summary>A single incoming document from AADE's RequestDocs endpoint.</summary>
public sealed record AadeInvoiceDto(
    string Mark,
    string IssuerAfm,
    string CounterpartyAfm,
    string CounterpartyName,
    DateTime IssueDate,
    string DocumentTypeCode,
    decimal NetAmount,
    decimal VatAmount,
    decimal GrossAmount,
    string Currency,
    string? CancelledByMark);

/// <summary>
/// An aggregated outgoing book summary from AADE's RequestMyIncome endpoint —
/// one per (counterparty, date, document type) tuple.
/// </summary>
public sealed record AadeBookEntryDto(
    string CounterpartyAfm,
    DateTime IssueDate,
    string DocumentTypeCode,
    decimal NetValue,
    decimal VatAmount,
    decimal GrossValue,
    int InvoiceCount,
    long MinMark,
    long MaxMark);
