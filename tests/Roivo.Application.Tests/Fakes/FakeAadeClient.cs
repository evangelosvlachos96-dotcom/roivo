using Roivo.Application.Abstractions.Aade;
using Roivo.Application.Abstractions.Aade.Results;

namespace Roivo.Application.Tests.Fakes;

/// <summary>
/// Programmable fake AADE client. Tests set <see cref="NextValidation"/> and
/// <see cref="NextFetch"/> to control the response of the next call.
/// </summary>
public sealed class FakeAadeClient : IAadeClient
{
    public AadeValidationResult NextValidation { get; set; } = new AadeValidationResult.Success();
    public AadeFetchResult NextFetch { get; set; } = new AadeFetchResult.Success(
        Array.Empty<AadeInvoiceDto>(), Array.Empty<AadeBookEntryDto>(), MaxIncomingMark: 0, MaxOutgoingMark: 0);

    public List<(string UserId, string SubscriptionKey, string BusinessAfm)> ValidationCalls { get; } = new();
    public List<(string UserId, string SubscriptionKey, long SinceIncomingMark, long SinceOutgoingMark)> FetchCalls { get; } = new();

    public Task<AadeValidationResult> ValidateCredentialsAsync(
        string userId,
        string subscriptionKey,
        string businessAfm,
        CancellationToken cancellationToken = default)
    {
        ValidationCalls.Add((userId, subscriptionKey, businessAfm));
        return Task.FromResult(NextValidation);
    }

    public Task<AadeFetchResult> FetchInvoicesAsync(
        string userId,
        string subscriptionKey,
        long sinceIncomingMark,
        long sinceOutgoingMark,
        CancellationToken cancellationToken = default)
    {
        FetchCalls.Add((userId, subscriptionKey, sinceIncomingMark, sinceOutgoingMark));
        return Task.FromResult(NextFetch);
    }
}
