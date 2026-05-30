using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Roivo.Aade.Configuration;
using Roivo.Application.Abstractions.Aade;
using Roivo.Application.Abstractions.Aade.Results;
using System.Globalization;
using System.Net;
using System.Xml.Linq;

namespace Roivo.Aade;

/// <summary>
/// HTTP-backed <see cref="IAadeClient"/> for the AADE myDATA API. Uses the
/// named HttpClient "Aade" configured with retry policies in
/// <see cref="Configuration.AadeServiceCollectionExtensions"/>.
/// </summary>
public sealed class AadeHttpClient : IAadeClient
{
    // Fragment matching is intentionally lenient — AADE's exact wording varies
    // slightly between versions, but these substrings are stable across
    // documented references.
    private const string InvalidKeyFragment = "invalid subscription key";
    private const string AfmNotAuthorizedFragment = "is not authorized to execute this method";

    // Captures the AFM that AADE reports as actually owning the credentials,
    // from messages like "User VAT number 123456783 is not authorized…".
    private static readonly System.Text.RegularExpressions.Regex AfmFromErrorRegex
        = new(@"User VAT number\s+(\d{9})\s+is not authorized",
              System.Text.RegularExpressions.RegexOptions.IgnoreCase
              | System.Text.RegularExpressions.RegexOptions.Compiled);

    private readonly HttpClient _http;
    private readonly ILogger<AadeHttpClient> _logger;
    private readonly AadeSettings _settings;

    public AadeHttpClient(HttpClient http, ILogger<AadeHttpClient> logger, IOptions<AadeSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(settings);
        _http = http;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<AadeValidationResult> ValidateCredentialsAsync(
        string userId,
        string subscriptionKey,
        string businessAfm,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(subscriptionKey);
        ArgumentNullException.ThrowIfNull(businessAfm);

        // RequestDocs with mark=0 and the supplied entityVatNumber probes both
        // credential validity AND AFM authorization in one call. AADE may return
        // 401/403 with distinct error fragments, OR return 200 with an error
        // payload inside the XML body — we must inspect the body in both cases.
        var url = $"{_settings.RequestDocsPath}?mark=0&entityVatNumber={Uri.EscapeDataString(businessAfm)}";

        using var request = BuildAuthorizedRequest(HttpMethod.Get, url, userId, subscriptionKey);

        HttpResponseMessage? response = null;
        try
        {
            response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);

            var statusCode = (int)response.StatusCode;
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            // Rate limit first — both 429 with empty body and 200 with rate-limit
            // text in body should be treated the same.
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                return new AadeValidationResult.RateLimited();

            // Server errors — propagate.
            if (statusCode >= 500)
                return new AadeValidationResult.AadeServerError(statusCode, response.ReasonPhrase ?? "Unknown");

            // Body-based error detection — runs regardless of HTTP status because
            // AADE sometimes returns 200 with error content. Order matters: more
            // specific checks first.
            if (responseBody.Contains(AfmNotAuthorizedFragment, StringComparison.OrdinalIgnoreCase))
            {
                var match = AfmFromErrorRegex.Match(responseBody);
                var credentialsAfm = match.Success ? match.Groups[1].Value : null;
                return new AadeValidationResult.AfmMismatch(credentialsAfm);
            }

            if (responseBody.Contains(InvalidKeyFragment, StringComparison.OrdinalIgnoreCase))
                return new AadeValidationResult.InvalidCredentials();

            // 401/403 without a matched fragment — assume credentials issue.
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return new AadeValidationResult.InvalidCredentials();

            // Other 4xx — surprise; treat as server error so the user retries later.
            if (statusCode >= 400)
                return new AadeValidationResult.AadeServerError(statusCode, response.ReasonPhrase ?? "Unknown");

            // 2xx without any error fragment in body — genuine success.
            return new AadeValidationResult.Success();
        }
        catch (HttpRequestException ex)
        {
            return new AadeValidationResult.NetworkError(ex.Message);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return new AadeValidationResult.NetworkError($"Timeout: {ex.Message}");
        }
        finally
        {
            response?.Dispose();
        }
    }

    public async Task<AadeFetchResult> FetchInvoicesAsync(
        string userId,
        string subscriptionKey,
        long sinceIncomingMark,
        long sinceOutgoingMark,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(subscriptionKey);

        var incomingTask = FetchAndParseAsync(BuildDocsUrl(sinceIncomingMark), userId, subscriptionKey, ParseIncomingInvoices, cancellationToken);
        var outgoingTask = FetchAndParseAsync(BuildIncomeUrl(sinceOutgoingMark), userId, subscriptionKey, ParseOutgoingBookEntries, cancellationToken);

        try
        {
            var incoming = await incomingTask.ConfigureAwait(false);
            var outgoing = await outgoingTask.ConfigureAwait(false);

            // If BOTH sides failed, propagate the first failure.
            if (incoming.Failure is not null && outgoing.Failure is not null)
                return incoming.Failure;

            // If only one side failed, log it and proceed with what we got —
            // AADE can have one endpoint degraded while the other works. A
            // failed side has an empty list, so its max-mark stays at the since
            // value and we don't advance the cursor past unfetched data.
            if (incoming.Failure is not null)
                _logger.LogWarning("AADE incoming fetch failed but outgoing succeeded; proceeding with partial data: {Failure}", incoming.Failure);
            if (outgoing.Failure is not null)
                _logger.LogWarning("AADE outgoing fetch failed but incoming succeeded; proceeding with partial data: {Failure}", outgoing.Failure);

            var incomingList = incoming.Items;
            var outgoingList = outgoing.Items;

            // Compute max mark per side. Default to the "since" value when no
            // items returned (means our cursor is already at the latest).
            var maxIncoming = incomingList.Count == 0
                ? sinceIncomingMark
                : incomingList.Max(i => long.TryParse(i.Mark, out var m) ? m : 0);

            var maxOutgoing = outgoingList.Count == 0
                ? sinceOutgoingMark
                : outgoingList.Max(b => b.MaxMark);

            return new AadeFetchResult.Success(
                Incoming: incomingList,
                Outgoing: outgoingList,
                MaxIncomingMark: maxIncoming,
                MaxOutgoingMark: maxOutgoing);
        }
        catch (HttpRequestException ex)
        {
            return new AadeFetchResult.NetworkError(ex.Message);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return new AadeFetchResult.NetworkError($"Timeout: {ex.Message}");
        }
    }

    // Shared fetch-with-diagnostic-log scaffolding; the per-endpoint XML shape
    // is handled by the supplied parser delegate.
    private async Task<(IReadOnlyList<T> Items, AadeFetchResult? Failure)> FetchAndParseAsync<T>(
        string url,
        string userId,
        string subscriptionKey,
        Func<string, IReadOnlyList<T>> parser,
        CancellationToken cancellationToken)
    {
        using var request = BuildAuthorizedRequest(HttpMethod.Get, url, userId, subscriptionKey);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Polly's retry policy can surface a transformed exception before we
            // ever see a response; make sure the URL is logged either way.
            _logger.LogWarning(ex, "AADE fetch threw before getting a response: Url={Url}", request.RequestUri);
            throw;
        }

        using (response)
        {
            var statusCode = (int)response.StatusCode;
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            // TEMPORARY DIAGNOSTIC: never logs the subscription key or user id —
            // only the URL we hit and what AADE returned. Remove once the sync 400 is fixed.
            _logger.LogInformation(
                "AADE fetch response: Url={Url}, HttpStatus={StatusCode}, BodyLength={BodyLength}, BodyPreview={BodyPreview}",
                request.RequestUri,
                statusCode,
                responseBody.Length,
                responseBody.Length > 1500 ? responseBody.Substring(0, 1500) : responseBody);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return (Array.Empty<T>(), new AadeFetchResult.InvalidCredentials());
            if (!response.IsSuccessStatusCode)
                return (Array.Empty<T>(), new AadeFetchResult.AadeServerError(statusCode, response.ReasonPhrase ?? "Unknown"));

            return (parser(responseBody), null);
        }
    }

    private static HttpRequestMessage BuildAuthorizedRequest(HttpMethod method, string url, string userId, string subscriptionKey)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("aade-user-id", userId);
        request.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
        return request;
    }

    // RequestMyIncome requires dateFrom AND dateTo even when paginating by mark;
    // AADE applies all filters, so incremental mark-based sync still works.
    // dateTo is computed at call time ("now"); dateFrom is the fixed epoch.
    private string BuildIncomeUrl(long sinceMark)
    {
        var dateTo = DateTime.UtcNow.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        return $"{_settings.RequestMyIncomePath}?mark={sinceMark}&dateFrom={_settings.IncomeEpochDate}&dateTo={dateTo}";
    }

    // RequestDocs requires only mark — no dateFrom needed.
    private string BuildDocsUrl(long sinceMark)
        => $"{_settings.RequestDocsPath}?mark={sinceMark}";

    private static IReadOnlyList<AadeInvoiceDto> ParseIncomingInvoices(string xml)
    {
        var results = new List<AadeInvoiceDto>();
        XDocument doc;
        try
        {
            doc = XDocument.Parse(xml);
        }
        catch (System.Xml.XmlException)
        {
            return results;
        }

        foreach (var invoice in doc.Descendants().Where(e => e.Name.LocalName == "invoice"))
        {
            var mark = (invoice.Descendants().FirstOrDefault(e => e.Name.LocalName == "mark")?.Value ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(mark)) continue;

            // A cancellation document voids this invoice; null when not cancelled.
            var cancelledByMark = invoice.Descendants().FirstOrDefault(e => e.Name.LocalName == "cancelledByMark")?.Value?.Trim();

            var issuer = invoice.Descendants().FirstOrDefault(e => e.Name.LocalName == "issuer");
            var issuerAfm = (issuer?.Descendants().FirstOrDefault(e => e.Name.LocalName == "vatNumber")?.Value ?? string.Empty).Trim();

            var counterpart = invoice.Descendants().FirstOrDefault(e => e.Name.LocalName == "counterpart");
            var counterAfm = (counterpart?.Descendants().FirstOrDefault(e => e.Name.LocalName == "vatNumber")?.Value ?? string.Empty).Trim();
            var counterName = (counterpart?.Descendants().FirstOrDefault(e => e.Name.LocalName == "name")?.Value ?? string.Empty).Trim();

            var header = invoice.Descendants().FirstOrDefault(e => e.Name.LocalName == "invoiceHeader");
            var issueDateText = header?.Descendants().FirstOrDefault(e => e.Name.LocalName == "issueDate")?.Value;
            var invoiceType = (header?.Descendants().FirstOrDefault(e => e.Name.LocalName == "invoiceType")?.Value ?? string.Empty).Trim();
            var currency = (header?.Descendants().FirstOrDefault(e => e.Name.LocalName == "currency")?.Value ?? "EUR").Trim();

            var summary = invoice.Descendants().FirstOrDefault(e => e.Name.LocalName == "invoiceSummary");
            var netText = summary?.Descendants().FirstOrDefault(e => e.Name.LocalName == "totalNetValue")?.Value;
            var vatText = summary?.Descendants().FirstOrDefault(e => e.Name.LocalName == "totalVatAmount")?.Value;
            var grossText = summary?.Descendants().FirstOrDefault(e => e.Name.LocalName == "totalGrossValue")?.Value;

            // AADE returns calendar dates (no time, no timezone). Interpret as UTC
            // midnight — Postgres timestamptz columns reject non-UTC DateTime.Kind.
            var issueDate = DateTime.MinValue;
            if (DateTime.TryParse(issueDateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                issueDate = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
            decimal.TryParse(netText, NumberStyles.Number, CultureInfo.InvariantCulture, out var net);
            decimal.TryParse(vatText, NumberStyles.Number, CultureInfo.InvariantCulture, out var vat);
            decimal.TryParse(grossText, NumberStyles.Number, CultureInfo.InvariantCulture, out var gross);

            results.Add(new AadeInvoiceDto(
                Mark: mark,
                IssuerAfm: issuerAfm,
                CounterpartyAfm: counterAfm,
                CounterpartyName: counterName,
                IssueDate: issueDate,
                DocumentTypeCode: invoiceType,
                NetAmount: net,
                VatAmount: vat,
                GrossAmount: gross,
                Currency: currency,
                CancelledByMark: string.IsNullOrWhiteSpace(cancelledByMark) ? null : cancelledByMark));
        }

        return results;
    }

    private static IReadOnlyList<AadeBookEntryDto> ParseOutgoingBookEntries(string xml)
    {
        var results = new List<AadeBookEntryDto>();
        XDocument doc;
        try { doc = XDocument.Parse(xml); }
        catch (System.Xml.XmlException) { return results; }

        foreach (var book in doc.Descendants().Where(e => e.Name.LocalName == "bookInfo"))
        {
            var counterAfm = (book.Descendants().FirstOrDefault(e => e.Name.LocalName == "counterVatNumber")?.Value ?? string.Empty).Trim();
            var issueDateText = book.Descendants().FirstOrDefault(e => e.Name.LocalName == "issueDate")?.Value;
            var invType = (book.Descendants().FirstOrDefault(e => e.Name.LocalName == "invType")?.Value ?? string.Empty).Trim();
            var netText = book.Descendants().FirstOrDefault(e => e.Name.LocalName == "netValue")?.Value;
            var vatText = book.Descendants().FirstOrDefault(e => e.Name.LocalName == "vatAmount")?.Value;
            var grossText = book.Descendants().FirstOrDefault(e => e.Name.LocalName == "grossValue")?.Value;
            var countText = book.Descendants().FirstOrDefault(e => e.Name.LocalName == "count")?.Value;
            var minMarkText = book.Descendants().FirstOrDefault(e => e.Name.LocalName == "minMark")?.Value;
            var maxMarkText = book.Descendants().FirstOrDefault(e => e.Name.LocalName == "maxMark")?.Value;

            if (string.IsNullOrEmpty(counterAfm) || string.IsNullOrEmpty(invType)) continue;

            // AADE returns calendar dates (no time, no timezone). Interpret as UTC
            // midnight — Postgres timestamptz columns reject non-UTC DateTime.Kind.
            var issueDate = DateTime.MinValue;
            if (DateTime.TryParse(issueDateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                issueDate = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
            decimal.TryParse(netText, NumberStyles.Number, CultureInfo.InvariantCulture, out var net);
            decimal.TryParse(vatText, NumberStyles.Number, CultureInfo.InvariantCulture, out var vat);
            decimal.TryParse(grossText, NumberStyles.Number, CultureInfo.InvariantCulture, out var gross);
            int.TryParse(countText, out var count);
            long.TryParse(minMarkText, out var minMark);
            long.TryParse(maxMarkText, out var maxMark);

            results.Add(new AadeBookEntryDto(
                CounterpartyAfm: counterAfm,
                IssueDate: issueDate,
                DocumentTypeCode: invType,
                NetValue: net,
                VatAmount: vat,
                GrossValue: gross,
                InvoiceCount: count,
                MinMark: minMark,
                MaxMark: maxMark));
        }

        return results;
    }
}
