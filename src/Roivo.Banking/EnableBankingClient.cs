using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Roivo.Application.Abstractions.Banking;
using Roivo.Application.Abstractions.Banking.Results;
using Roivo.Banking.Configuration;
using Roivo.Banking.Models;

namespace Roivo.Banking;

/// <summary>
/// HTTP-backed <see cref="IBankingClient"/> for the Enable Banking PSD2 API.
/// Registered as a typed client with a Polly retry policy in
/// <see cref="Configuration.EnableBankingServiceCollectionExtensions"/>.
/// </summary>
public sealed class EnableBankingClient : IBankingClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http;
    private readonly EnableBankingJwtFactory _jwt;
    private readonly ILogger<EnableBankingClient> _logger;
    private readonly EnableBankingSettings _settings;

    public EnableBankingClient(
        HttpClient http,
        EnableBankingJwtFactory jwt,
        ILogger<EnableBankingClient> logger,
        IOptions<EnableBankingSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(jwt);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(settings);

        _http = http;
        _jwt = jwt;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<BankingProvidersResult> ListProvidersAsync(
        string country,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(country);

        try
        {
            using var request = BuildRequest(HttpMethod.Get, $"aspsps?country={Uri.EscapeDataString(country)}");
            using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                    ? new BankingProvidersResult.Unauthorized(DescribeError(body, response))
                    : new BankingProvidersResult.BankingServerError((int)response.StatusCode, DescribeError(body, response));
            }

            var parsed = Deserialize<ProviderListDto>(body);
            var providers = (parsed?.Aspsps ?? [])
                .Where(a => !string.IsNullOrWhiteSpace(a.Name))
                .Select(a => new BankingProviderDto(
                    Name: a.Name!,
                    Country: a.Country ?? country,
                    LogoUrl: a.Logo,
                    PsuTypes: a.PsuTypes ?? []))
                .ToList();

            return new BankingProvidersResult.Success(providers);
        }
        catch (HttpRequestException ex)
        {
            return new BankingProvidersResult.NetworkError(ex.Message);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return new BankingProvidersResult.NetworkError($"Timeout: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return new BankingProvidersResult.BankingServerError(0, $"Unreadable response: {ex.Message}");
        }
    }

    public async Task<BankingAuthorizationResult> StartAuthorizationAsync(
        BankingAuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var payload = new StartAuthDto
        {
            Access = new AccessDto
            {
                ValidUntil = request.AccessValidUntil.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
            },
            Aspsp = new AspspRefDto { Name = request.BankName, Country = request.Country },
            State = request.State,
            RedirectUrl = request.RedirectUrl,
            PsuType = request.PsuType,
            Language = request.Language,
        };

        try
        {
            using var httpRequest = BuildRequest(HttpMethod.Post, "auth");
            httpRequest.Content = JsonContent.Create(payload, options: JsonOptions);

            using var response = await _http.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                    return new BankingAuthorizationResult.Unauthorized(DescribeError(body, response));

                // Enable Banking answers an unknown ASPSP name with 404, and a
                // known-but-unusable one with 422. Both mean "pick another bank",
                // which is a different fix from "try again later".
                if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.UnprocessableContent)
                    return new BankingAuthorizationResult.UnknownProvider(request.BankName, request.Country);

                return new BankingAuthorizationResult.BankingServerError((int)response.StatusCode, DescribeError(body, response));
            }

            var parsed = Deserialize<StartAuthResponseDto>(body);
            if (parsed?.Url is null || parsed.AuthorizationId is null)
                return new BankingAuthorizationResult.BankingServerError((int)response.StatusCode, "Authorization response had no redirect URL.");

            return new BankingAuthorizationResult.Success(parsed.Url, parsed.AuthorizationId);
        }
        catch (HttpRequestException ex)
        {
            return new BankingAuthorizationResult.NetworkError(ex.Message);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return new BankingAuthorizationResult.NetworkError($"Timeout: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return new BankingAuthorizationResult.BankingServerError(0, $"Unreadable response: {ex.Message}");
        }
    }

    public Task<BankingSessionResult> CompleteAuthorizationAsync(
        string authorizationCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authorizationCode);

        return SendSessionRequestAsync(
            () =>
            {
                var request = BuildRequest(HttpMethod.Post, "sessions");
                request.Content = JsonContent.Create(new CreateSessionDto { Code = authorizationCode }, options: JsonOptions);
                return request;
            },
            // A bad code comes back as 400 or 401 from POST /sessions. Treat both
            // as InvalidCode: our own JWT was accepted for the request to get here.
            treatUnauthorizedAsInvalidCode: true,
            cancellationToken);
    }

    public Task<BankingSessionResult> GetSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        return SendSessionRequestAsync(
            () => BuildRequest(HttpMethod.Get, $"sessions/{Uri.EscapeDataString(sessionId)}"),
            treatUnauthorizedAsInvalidCode: false,
            cancellationToken);
    }

    public async Task<BankingFetchResult> FetchTransactionsAsync(
        string accountUid,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountUid);

        if (to < from)
            throw new ArgumentException($"'{nameof(to)}' must not precede '{nameof(from)}'.", nameof(to));

        var collected = new List<BankingTransactionDto>();
        string? continuationKey = null;

        try
        {
            for (var page = 0; page < _settings.MaxTransactionPages; page++)
            {
                var url = BuildTransactionsUrl(accountUid, from, to, continuationKey);
                using var request = BuildRequest(HttpMethod.Get, url);
                using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    // 401 here means the bank-side consent lapsed, not that our
                    // application credentials are wrong — those would fail at
                    // every other endpoint too.
                    if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
                        return new BankingFetchResult.SessionExpired();

                    return new BankingFetchResult.BankingServerError((int)response.StatusCode, DescribeError(body, response));
                }

                var parsed = Deserialize<TransactionPageDto>(body);
                foreach (var dto in parsed?.Transactions ?? [])
                {
                    var mapped = MapTransaction(dto);
                    if (mapped is not null)
                        collected.Add(mapped);
                }

                continuationKey = parsed?.ContinuationKey;
                if (string.IsNullOrEmpty(continuationKey))
                    return new BankingFetchResult.Success(collected);
            }

            _logger.LogWarning(
                "Enable Banking transactions for account {AccountUid} still paginating after {MaxPages} pages; returning the {Count} collected so far",
                accountUid, _settings.MaxTransactionPages, collected.Count);

            return new BankingFetchResult.Success(collected);
        }
        catch (HttpRequestException ex)
        {
            return new BankingFetchResult.NetworkError(ex.Message);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return new BankingFetchResult.NetworkError($"Timeout: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return new BankingFetchResult.BankingServerError(0, $"Unreadable response: {ex.Message}");
        }
    }

    public async Task<BankingRevokeResult> RevokeSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        try
        {
            using var request = BuildRequest(HttpMethod.Delete, $"sessions/{Uri.EscapeDataString(sessionId)}");
            using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
                return new BankingRevokeResult.Success();

            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
                return new BankingRevokeResult.AlreadyGone();

            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return new BankingRevokeResult.Failed(DescribeError(body, response));
        }
        catch (HttpRequestException ex)
        {
            return new BankingRevokeResult.Failed(ex.Message);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return new BankingRevokeResult.Failed($"Timeout: {ex.Message}");
        }
    }

    private async Task<BankingSessionResult> SendSessionRequestAsync(
        Func<HttpRequestMessage> requestFactory,
        bool treatUnauthorizedAsInvalidCode,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = requestFactory();
            using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode is HttpStatusCode.BadRequest)
                    return new BankingSessionResult.InvalidCode();

                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                {
                    return treatUnauthorizedAsInvalidCode
                        ? new BankingSessionResult.InvalidCode()
                        : new BankingSessionResult.SessionExpired();
                }

                if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
                    return new BankingSessionResult.SessionExpired();

                return new BankingSessionResult.BankingServerError((int)response.StatusCode, DescribeError(body, response));
            }

            var parsed = Deserialize<SessionDto>(body);
            if (parsed?.SessionId is null)
                return new BankingSessionResult.BankingServerError((int)response.StatusCode, "Session response had no session id.");

            var accounts = (parsed.Accounts ?? [])
                .Where(a => !string.IsNullOrWhiteSpace(a.Uid))
                .Select(a => new BankingAccountDto(
                    Uid: a.Uid!,
                    Iban: a.AccountId?.Iban ?? a.AccountId?.Other?.Identification,
                    Name: a.Name,
                    Currency: string.IsNullOrWhiteSpace(a.Currency) ? "EUR" : a.Currency,
                    Product: a.Product ?? a.CashAccountType))
                .ToList();

            return new BankingSessionResult.Success(
                SessionId: parsed.SessionId,
                BankName: parsed.Aspsp?.Name ?? string.Empty,
                Country: parsed.Aspsp?.Country ?? _settings.Country,
                AccessValidUntil: parsed.Access?.ValidUntil,
                Accounts: accounts);
        }
        catch (HttpRequestException ex)
        {
            return new BankingSessionResult.NetworkError(ex.Message);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return new BankingSessionResult.NetworkError($"Timeout: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return new BankingSessionResult.BankingServerError(0, $"Unreadable response: {ex.Message}");
        }
    }

    private static string BuildTransactionsUrl(string accountUid, DateOnly from, DateOnly to, string? continuationKey)
    {
        var url = $"accounts/{Uri.EscapeDataString(accountUid)}/transactions"
                + $"?date_from={from:yyyy-MM-dd}&date_to={to:yyyy-MM-dd}&transaction_status=BOOK";

        return continuationKey is null
            ? url
            : $"{url}&continuation_key={Uri.EscapeDataString(continuationKey)}";
    }

    /// <summary>
    /// Maps one wire transaction to the neutral DTO, or null when it carries no
    /// usable identity, amount or date. Enable Banking passes through whatever
    /// the bank sent, so a malformed row is a bank quirk — drop it rather than
    /// fail the whole page.
    /// </summary>
    private BankingTransactionDto? MapTransaction(TransactionDto dto)
    {
        var externalId = dto.EntryReference ?? dto.TransactionId;
        if (string.IsNullOrWhiteSpace(externalId))
        {
            _logger.LogWarning("Skipping Enable Banking transaction with no entry_reference or transaction_id");
            return null;
        }

        if (!decimal.TryParse(dto.TransactionAmount?.Amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var magnitude))
        {
            _logger.LogWarning("Skipping Enable Banking transaction {ExternalId}: unparseable amount", externalId);
            return null;
        }

        var bookingDate = dto.BookingDate ?? dto.ValueDate ?? dto.TransactionDate;
        if (bookingDate is null)
        {
            _logger.LogWarning("Skipping Enable Banking transaction {ExternalId}: no booking, value or transaction date", externalId);
            return null;
        }

        // Enable Banking sends the magnitude unsigned and the direction
        // separately; Roivo stores a signed amount.
        var isCredit = string.Equals(dto.CreditDebitIndicator, EnableBankingWire.CreditIndicator, StringComparison.OrdinalIgnoreCase);
        var amount = isCredit ? Math.Abs(magnitude) : -Math.Abs(magnitude);

        // The counterparty is whichever side of the transfer isn't us: on money
        // in we were the creditor, so the debtor is the other party.
        var counterpartyName = isCredit ? dto.Debtor?.Name : dto.Creditor?.Name;
        var counterpartyAccount = isCredit ? dto.DebtorAccount : dto.CreditorAccount;

        var reference = dto.RemittanceInformation is { Length: > 0 }
            ? string.Join(" ", dto.RemittanceInformation.Where(r => !string.IsNullOrWhiteSpace(r)))
            : null;

        return new BankingTransactionDto(
            ExternalId: externalId,
            BookingDate: bookingDate.Value,
            ValueDate: dto.ValueDate,
            Amount: amount,
            Currency: dto.TransactionAmount?.Currency ?? "EUR",
            CounterpartyName: NullIfBlank(counterpartyName),
            CounterpartyIban: NullIfBlank(counterpartyAccount?.Iban ?? counterpartyAccount?.Other?.Identification),
            Reference: NullIfBlank(reference),
            RawPayload: JsonSerializer.Serialize(dto, JsonOptions));
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string relativeUrl)
    {
        var request = new HttpRequestMessage(method, relativeUrl);
        // Minted per request: the token lives minutes, so there is no cached
        // credential to invalidate and no refresh path to get wrong.
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _jwt.CreateToken());
        return request;
    }

    private static T? Deserialize<T>(string body)
        => string.IsNullOrWhiteSpace(body) ? default : JsonSerializer.Deserialize<T>(body, JsonOptions);

    /// <summary>
    /// Builds a diagnostic string from an error body. Enable Banking's error
    /// envelope carries no PSU data, so this is safe to log and to surface in a
    /// result.
    /// </summary>
    private static string DescribeError(string body, HttpResponseMessage response)
    {
        try
        {
            var parsed = Deserialize<ApiErrorDto>(body);
            var message = parsed?.Message ?? parsed?.Error;
            if (!string.IsNullOrWhiteSpace(message))
                return parsed?.Code is { Length: > 0 } code ? $"{code}: {message}" : message;
        }
        catch (JsonException)
        {
            // Not every error body is JSON — gateway HTML and empty 502s happen.
        }

        if (!string.IsNullOrWhiteSpace(body))
            return body.Length > 300 ? body[..300] : body;

        return response.ReasonPhrase ?? response.StatusCode.ToString();
    }

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
