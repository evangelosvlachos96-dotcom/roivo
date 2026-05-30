using Roivo.Application.Abstractions.Aade.Results;

namespace Roivo.Application.Abstractions.Aade;

/// <summary>
/// Talks to the AADE myDATA API. Implementations are HTTP clients in
/// Roivo.Aade; tests substitute in-memory fakes.
/// </summary>
public interface IAadeClient
{
    /// <summary>
    /// Validates credentials AND authorization for the given AFM. AADE's
    /// RequestDocs endpoint distinguishes invalid keys, unauthorized AFM,
    /// rate-limiting and server errors via specific HTTP codes / response
    /// fragments, so this is a single round-trip.
    /// </summary>
    Task<AadeValidationResult> ValidateCredentialsAsync(
        string userId,
        string subscriptionKey,
        string businessAfm,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches invoices newer than the supplied marks. AADE paginates by mark
    /// (a sequential per-invoice id): the endpoints return invoices with
    /// mark &gt; the supplied value; pass 0 to fetch from the start.
    /// </summary>
    Task<AadeFetchResult> FetchInvoicesAsync(
        string userId,
        string subscriptionKey,
        long sinceIncomingMark,
        long sinceOutgoingMark,
        CancellationToken cancellationToken = default);
}
