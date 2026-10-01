namespace Roivo.Application.Abstractions.Banking.Results;

/// <summary>Outcome of listing the banks an aggregator supports in a country.</summary>
public abstract record BankingProvidersResult
{
    public sealed record Success(IReadOnlyList<BankingProviderDto> Providers) : BankingProvidersResult;

    /// <summary>The aggregator rejected our own application credentials —
    /// a configuration problem on our side, not the user's.</summary>
    public sealed record Unauthorized(string Message) : BankingProvidersResult;

    public sealed record NetworkError(string Message) : BankingProvidersResult;
    public sealed record BankingServerError(int StatusCode, string Message) : BankingProvidersResult;
}

/// <summary>Outcome of starting a bank authorization.</summary>
public abstract record BankingAuthorizationResult
{
    /// <summary><c>AuthorizationUrl</c> is where the user must be sent to consent.</summary>
    public sealed record Success(string AuthorizationUrl, string AuthorizationId) : BankingAuthorizationResult;

    /// <summary>The requested bank is not offered by the aggregator for that country.</summary>
    public sealed record UnknownProvider(string BankName, string Country) : BankingAuthorizationResult;

    public sealed record Unauthorized(string Message) : BankingAuthorizationResult;
    public sealed record NetworkError(string Message) : BankingAuthorizationResult;
    public sealed record BankingServerError(int StatusCode, string Message) : BankingAuthorizationResult;
}

/// <summary>Outcome of exchanging an authorization code for a session, or of
/// re-reading an existing one.</summary>
public abstract record BankingSessionResult
{
    public sealed record Success(
        string SessionId,
        string BankName,
        string Country,
        DateTimeOffset? AccessValidUntil,
        IReadOnlyList<BankingAccountDto> Accounts) : BankingSessionResult;

    /// <summary>The code was already used, expired, or never issued by us.
    /// Distinct from <see cref="Unauthorized"/>: the user can recover by
    /// restarting the consent flow.</summary>
    public sealed record InvalidCode : BankingSessionResult;

    /// <summary>The session no longer exists or the bank-side consent lapsed.
    /// The user must re-consent.</summary>
    public sealed record SessionExpired : BankingSessionResult;

    public sealed record Unauthorized(string Message) : BankingSessionResult;
    public sealed record NetworkError(string Message) : BankingSessionResult;
    public sealed record BankingServerError(int StatusCode, string Message) : BankingSessionResult;
}

/// <summary>Outcome of fetching one account's transactions.</summary>
public abstract record BankingFetchResult
{
    public sealed record Success(IReadOnlyList<BankingTransactionDto> Transactions) : BankingFetchResult;
    public sealed record SessionExpired : BankingFetchResult;
    public sealed record Unauthorized(string Message) : BankingFetchResult;
    public sealed record NetworkError(string Message) : BankingFetchResult;
    public sealed record BankingServerError(int StatusCode, string Message) : BankingFetchResult;
}

/// <summary>Outcome of revoking a session. Revocation is best-effort — a
/// failure must not block the local disconnect.</summary>
public abstract record BankingRevokeResult
{
    public sealed record Success : BankingRevokeResult;

    /// <summary>Nothing to revoke — the session was already gone.</summary>
    public sealed record AlreadyGone : BankingRevokeResult;

    public sealed record Failed(string Message) : BankingRevokeResult;
}

/// <summary>A bank the aggregator can connect to.</summary>
public sealed record BankingProviderDto(
    string Name,
    string Country,
    string? LogoUrl,
    IReadOnlyList<string> PsuTypes);

/// <summary>
/// One account the user granted access to. <c>Uid</c> is the aggregator's
/// opaque handle and is the only key that works against the transactions
/// endpoint — the IBAN is for display.
/// </summary>
public sealed record BankingAccountDto(
    string Uid,
    string? Iban,
    string? Name,
    string Currency,
    string? Product);

/// <summary>
/// One bank transaction, normalised. <c>Amount</c> is signed — positive for
/// money in, negative for money out — because aggregators disagree about
/// whether to encode direction in the sign or in a separate indicator, and the
/// rest of Roivo should not have to care.
/// </summary>
public sealed record BankingTransactionDto(
    string ExternalId,
    DateOnly BookingDate,
    DateOnly? ValueDate,
    decimal Amount,
    string Currency,
    string? CounterpartyName,
    string? CounterpartyIban,
    string? Reference,
    string? RawPayload);
