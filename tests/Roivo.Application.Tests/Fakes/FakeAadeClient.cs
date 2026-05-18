using Roivo.Application.Abstractions.Aade;
using Roivo.Application.Abstractions.Aade.Results;

namespace Roivo.Application.Tests.Fakes;

/// <summary>
/// Programmable fake AADE client. Tests set <see cref="NextValidation"/> and
/// <see cref="NextFetch"/> to control the response of the next call.
/// </summary>
public sealed class FakeAadeClient : IAadeClient
{
    public AadeValidationResult NextValidation { get; set; } = new AadeValidationResult.Success("000000000");
    public AadeFetchResult NextFetch { get; set; } = new AadeFetchResult.Success(
        Array.Empty<AadeInvoiceDto>(), Array.Empty<AadeInvoiceDto>());

    public List<(string UserId, string SubscriptionKey)> ValidationCalls { get; } = new();
    public List<(string UserId, string SubscriptionKey, DateTime? SinceUtc)> FetchCalls { get; } = new();

    public Task<AadeValidationResult> ValidateCredentialsAsync(string userId, string subscriptionKey, CancellationToken cancellationToken = default)
    {
        ValidationCalls.Add((userId, subscriptionKey));
        return Task.FromResult(NextValidation);
    }

    public Task<AadeFetchResult> FetchInvoicesAsync(string userId, string subscriptionKey, DateTime? sinceUtc, CancellationToken cancellationToken = default)
    {
        FetchCalls.Add((userId, subscriptionKey, sinceUtc));
        return Task.FromResult(NextFetch);
    }
}
