using Roivo.Application.Abstractions.Banking.Results;

namespace Roivo.Application.Abstractions.Banking;

/// <summary>
/// Talks to a PSD2 account-information aggregator. Implementations are HTTP
/// clients in Roivo.Banking; tests substitute in-memory fakes.
/// </summary>
/// <remarks>
/// The shape is the generic PSD2 AIS flow — list banks, redirect the user to
/// their bank, exchange the returned code for a session, then read
/// transactions per account — not any one aggregator's vocabulary. Swapping
/// aggregators replaces the implementation, not this interface.
/// </remarks>
public interface IBankingClient
{
    /// <summary>
    /// Lists the banks the aggregator can connect to in the given ISO-3166
    /// country (e.g. "GR"). Doubles as a credentials smoke-test: it is the
    /// cheapest authenticated call the API offers.
    /// </summary>
    Task<BankingProvidersResult> ListProvidersAsync(
        string country,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts an authorization. Returns the URL the user must be redirected to
    /// so they can consent at their own bank. Nothing is persisted yet — the
    /// connection only exists once the user comes back with a code.
    /// </summary>
    Task<BankingAuthorizationResult> StartAuthorizationAsync(
        BankingAuthorizationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exchanges the authorization code from the post-consent redirect for a
    /// session plus the list of accounts the user granted access to.
    /// </summary>
    Task<BankingSessionResult> CompleteAuthorizationAsync(
        string authorizationCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-reads an existing session. Used to confirm a stored session is still
    /// live (consents expire or are revoked at the bank) without re-consenting.
    /// </summary>
    Task<BankingSessionResult> GetSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches booked transactions for one account over a closed date range.
    /// Implementations follow the aggregator's pagination internally and return
    /// the whole range.
    /// </summary>
    Task<BankingFetchResult> FetchTransactionsAsync(
        string accountUid,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a session at the aggregator so the bank-side consent is released
    /// when a user disconnects. Best-effort: failures are reported, not thrown.
    /// </summary>
    Task<BankingRevokeResult> RevokeSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default);
}

/// <summary>Everything needed to start one bank authorization.</summary>
public sealed record BankingAuthorizationRequest(
    string BankName,
    string Country,
    string RedirectUrl,
    string State,
    DateTimeOffset AccessValidUntil,
    string PsuType = "business",
    string? Language = null);
