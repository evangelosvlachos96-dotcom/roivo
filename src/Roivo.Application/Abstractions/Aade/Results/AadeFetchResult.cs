namespace Roivo.Application.Abstractions.Aade.Results;

/// <summary>Outcome of an AADE invoice-fetch call.</summary>
public abstract record AadeFetchResult
{
    public sealed record Success(
        IReadOnlyList<AadeInvoiceDto> Incoming,
        IReadOnlyList<AadeInvoiceDto> Outgoing) : AadeFetchResult;
    public sealed record InvalidCredentials : AadeFetchResult;
    public sealed record NetworkError(string Message) : AadeFetchResult;
    public sealed record AadeServerError(int StatusCode, string Message) : AadeFetchResult;
}

/// <summary>
/// DTO for a single invoice as returned by AADE. The full raw XML is preserved
/// in <see cref="RawXml"/> for forensics — if a downstream parse goes wrong we
/// always have the original payload to compare against.
/// </summary>
public sealed record AadeInvoiceDto(
    string Mark,
    string CounterpartyAfm,
    string CounterpartyName,
    DateTime IssueDate,
    string DocumentTypeCode,
    decimal GrossAmount,
    string Currency,
    string RawXml);
