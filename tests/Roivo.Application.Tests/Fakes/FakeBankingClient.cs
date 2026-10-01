using Roivo.Application.Abstractions.Banking;
using Roivo.Application.Abstractions.Banking.Results;

namespace Roivo.Application.Tests.Fakes;

/// <summary>
/// Programmable fake banking client. Tests set the Next* properties to control
/// the response of the next call; the *Calls lists record what was asked for.
/// </summary>
public sealed class FakeBankingClient : IBankingClient
{
    public BankingProvidersResult NextProviders { get; set; } =
        new BankingProvidersResult.Success([new BankingProviderDto("Test Bank", "GR", null, ["business"])]);

    public BankingAuthorizationResult NextAuthorization { get; set; } =
        new BankingAuthorizationResult.Success("https://auth.example/consent?id=1", "auth-1");

    public BankingSessionResult NextSession { get; set; } =
        new BankingSessionResult.Success(
            SessionId: "session-1",
            BankName: "Test Bank",
            Country: "GR",
            AccessValidUntil: new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero),
            Accounts: [new BankingAccountDto("acct-uid-1", "GR1601101250000000012300695", "Main", "EUR", "CACC")]);

    public BankingFetchResult NextFetch { get; set; } = new BankingFetchResult.Success([]);

    public BankingRevokeResult NextRevoke { get; set; } = new BankingRevokeResult.Success();

    public List<string> ProviderCalls { get; } = [];
    public List<BankingAuthorizationRequest> AuthorizationCalls { get; } = [];
    public List<string> CompleteCalls { get; } = [];
    public List<string> SessionCalls { get; } = [];
    public List<(string AccountUid, DateOnly From, DateOnly To)> FetchCalls { get; } = [];
    public List<string> RevokeCalls { get; } = [];

    public Task<BankingProvidersResult> ListProvidersAsync(string country, CancellationToken cancellationToken = default)
    {
        ProviderCalls.Add(country);
        return Task.FromResult(NextProviders);
    }

    public Task<BankingAuthorizationResult> StartAuthorizationAsync(BankingAuthorizationRequest request, CancellationToken cancellationToken = default)
    {
        AuthorizationCalls.Add(request);
        return Task.FromResult(NextAuthorization);
    }

    public Task<BankingSessionResult> CompleteAuthorizationAsync(string authorizationCode, CancellationToken cancellationToken = default)
    {
        CompleteCalls.Add(authorizationCode);
        return Task.FromResult(NextSession);
    }

    public Task<BankingSessionResult> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        SessionCalls.Add(sessionId);
        return Task.FromResult(NextSession);
    }

    public Task<BankingFetchResult> FetchTransactionsAsync(string accountUid, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        FetchCalls.Add((accountUid, from, to));
        return Task.FromResult(NextFetch);
    }

    public Task<BankingRevokeResult> RevokeSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        RevokeCalls.Add(sessionId);
        return Task.FromResult(NextRevoke);
    }
}
