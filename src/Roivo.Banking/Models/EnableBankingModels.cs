using System.Text.Json.Serialization;

namespace Roivo.Banking.Models;

/// <summary>
/// Wire shapes for the Enable Banking API. These mirror the JSON exactly and
/// never leave <c>Roivo.Banking</c> — <see cref="EnableBankingClient"/> maps
/// them to the aggregator-neutral DTOs in
/// <c>Roivo.Application.Abstractions.Banking.Results</c>.
/// </summary>
/// <remarks>
/// Everything is nullable because Enable Banking's coverage varies by bank:
/// a field the spec marks optional is genuinely absent for some Greek ASPSPs.
/// </remarks>
internal static class EnableBankingWire
{
    internal const string CreditIndicator = "CRDT";
    internal const string DebitIndicator = "DBIT";
}

/// <summary>A bank (ASPSP) from <c>GET /aspsps</c>.</summary>
internal sealed record ProviderDto
{
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("country")] public string? Country { get; init; }
    [JsonPropertyName("logo")] public string? Logo { get; init; }
    [JsonPropertyName("psu_types")] public string[]? PsuTypes { get; init; }
}

internal sealed record ProviderListDto
{
    [JsonPropertyName("aspsps")] public ProviderDto[]? Aspsps { get; init; }
}

/// <summary>Request body for <c>POST /auth</c>.</summary>
internal sealed record StartAuthDto
{
    [JsonPropertyName("access")] public required AccessDto Access { get; init; }
    [JsonPropertyName("aspsp")] public required AspspRefDto Aspsp { get; init; }
    [JsonPropertyName("state")] public required string State { get; init; }
    [JsonPropertyName("redirect_url")] public required string RedirectUrl { get; init; }
    [JsonPropertyName("psu_type")] public required string PsuType { get; init; }

    /// <summary>Null when the bank has no preference; the client's serializer
    /// drops null properties so the field simply isn't sent.</summary>
    [JsonPropertyName("language")] public string? Language { get; init; }
}

internal sealed record AccessDto
{
    /// <summary>ISO-8601 instant after which the consent lapses.</summary>
    [JsonPropertyName("valid_until")] public required string ValidUntil { get; init; }
    [JsonPropertyName("balances")] public bool Balances { get; init; } = true;
    [JsonPropertyName("transactions")] public bool Transactions { get; init; } = true;
}

internal sealed record AspspRefDto
{
    [JsonPropertyName("name")] public required string Name { get; init; }
    [JsonPropertyName("country")] public required string Country { get; init; }
}

/// <summary>Response from <c>POST /auth</c>.</summary>
internal sealed record StartAuthResponseDto
{
    [JsonPropertyName("url")] public string? Url { get; init; }
    [JsonPropertyName("authorization_id")] public string? AuthorizationId { get; init; }
}

/// <summary>Request body for <c>POST /sessions</c>.</summary>
internal sealed record CreateSessionDto
{
    [JsonPropertyName("code")] public required string Code { get; init; }
}

/// <summary>Response from <c>POST /sessions</c> and <c>GET /sessions/{id}</c>.</summary>
internal sealed record SessionDto
{
    [JsonPropertyName("session_id")] public string? SessionId { get; init; }
    [JsonPropertyName("status")] public string? Status { get; init; }
    [JsonPropertyName("accounts")] public AccountDto[]? Accounts { get; init; }
    [JsonPropertyName("aspsp")] public AspspRefDto? Aspsp { get; init; }
    [JsonPropertyName("access")] public SessionAccessDto? Access { get; init; }
}

internal sealed record SessionAccessDto
{
    [JsonPropertyName("valid_until")] public DateTimeOffset? ValidUntil { get; init; }
}

/// <summary>One authorised account inside a session.</summary>
internal sealed record AccountDto
{
    /// <summary>The only handle that works against the transactions endpoint.</summary>
    [JsonPropertyName("uid")] public string? Uid { get; init; }
    [JsonPropertyName("account_id")] public AccountIdDto? AccountId { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("currency")] public string? Currency { get; init; }
    [JsonPropertyName("product")] public string? Product { get; init; }
    [JsonPropertyName("cash_account_type")] public string? CashAccountType { get; init; }
}

internal sealed record AccountIdDto
{
    [JsonPropertyName("iban")] public string? Iban { get; init; }
    [JsonPropertyName("other")] public OtherAccountIdDto? Other { get; init; }
}

internal sealed record OtherAccountIdDto
{
    [JsonPropertyName("identification")] public string? Identification { get; init; }
}

/// <summary>One page of <c>GET /accounts/{uid}/transactions</c>.</summary>
internal sealed record TransactionPageDto
{
    [JsonPropertyName("transactions")] public TransactionDto[]? Transactions { get; init; }

    /// <summary>Opaque cursor; absent on the last page.</summary>
    [JsonPropertyName("continuation_key")] public string? ContinuationKey { get; init; }
}

/// <summary>A raw bank transaction.</summary>
internal sealed record TransactionDto
{
    [JsonPropertyName("entry_reference")] public string? EntryReference { get; init; }
    [JsonPropertyName("transaction_amount")] public AmountDto? TransactionAmount { get; init; }

    /// <summary>"CRDT" (money in) or "DBIT" (money out).</summary>
    [JsonPropertyName("credit_debit_indicator")] public string? CreditDebitIndicator { get; init; }

    [JsonPropertyName("status")] public string? Status { get; init; }
    [JsonPropertyName("booking_date")] public DateOnly? BookingDate { get; init; }
    [JsonPropertyName("value_date")] public DateOnly? ValueDate { get; init; }
    [JsonPropertyName("transaction_date")] public DateOnly? TransactionDate { get; init; }
    [JsonPropertyName("creditor")] public PartyDto? Creditor { get; init; }
    [JsonPropertyName("creditor_account")] public AccountIdDto? CreditorAccount { get; init; }
    [JsonPropertyName("debtor")] public PartyDto? Debtor { get; init; }
    [JsonPropertyName("debtor_account")] public AccountIdDto? DebtorAccount { get; init; }
    [JsonPropertyName("remittance_information")] public string[]? RemittanceInformation { get; init; }
    [JsonPropertyName("transaction_id")] public string? TransactionId { get; init; }
}

internal sealed record AmountDto
{
    /// <summary>Decimal as a string — Enable Banking never sends a JSON number
    /// for money, to avoid float rounding.</summary>
    [JsonPropertyName("amount")] public string? Amount { get; init; }
    [JsonPropertyName("currency")] public string? Currency { get; init; }
}

internal sealed record PartyDto
{
    [JsonPropertyName("name")] public string? Name { get; init; }
}

/// <summary>Enable Banking's error envelope.</summary>
internal sealed record ApiErrorDto
{
    [JsonPropertyName("message")] public string? Message { get; init; }
    [JsonPropertyName("error")] public string? Error { get; init; }
    [JsonPropertyName("code")] public string? Code { get; init; }
}
