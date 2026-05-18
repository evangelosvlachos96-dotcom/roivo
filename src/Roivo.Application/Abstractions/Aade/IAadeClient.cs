using Roivo.Application.Abstractions.Aade.Results;

namespace Roivo.Application.Abstractions.Aade;

/// <summary>
/// Talks to the AADE myDATA API. Implementations are HTTP clients in
/// Roivo.Aade; tests substitute in-memory fakes.
/// </summary>
public interface IAadeClient
{
    Task<AadeValidationResult> ValidateCredentialsAsync(
        string userId,
        string subscriptionKey,
        CancellationToken cancellationToken = default);

    Task<AadeFetchResult> FetchInvoicesAsync(
        string userId,
        string subscriptionKey,
        DateTime? sinceUtc,
        CancellationToken cancellationToken = default);
}
