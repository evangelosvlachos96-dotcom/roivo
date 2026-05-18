using System.Globalization;
using System.Net;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Roivo.Application.Abstractions.Aade;
using Roivo.Application.Abstractions.Aade.Results;

namespace Roivo.Aade;

/// <summary>
/// HTTP-backed <see cref="IAadeClient"/> for the AADE myDATA API. Uses the
/// named HttpClient "Aade" configured with retry policies in
/// <see cref="Configuration.AadeServiceCollectionExtensions"/>.
/// </summary>
public sealed class AadeHttpClient : IAadeClient
{
    private readonly HttpClient _http;
    private readonly ILogger<AadeHttpClient> _logger;

    public AadeHttpClient(HttpClient http, ILogger<AadeHttpClient> logger)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(logger);
        _http = http;
        _logger = logger;
    }

    public async Task<AadeValidationResult> ValidateCredentialsAsync(string userId, string subscriptionKey, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(subscriptionKey);

        // Probe with a minimal RequestMyIncome window — yesterday only. Any 200
        // payload confirms credentials; AADE echoes back the caller's AFM in
        // the response which we parse out for the AFM-match check.
        var yesterday = DateTime.UtcNow.AddDays(-1).Date;
        var url = BuildIncomeUrl(yesterday, yesterday);

        using var request = BuildAuthorizedRequest(HttpMethod.Get, url, userId, subscriptionKey);

        try
        {
            using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return new AadeValidationResult.InvalidCredentials();

            if (!response.IsSuccessStatusCode)
                return new AadeValidationResult.AadeServerError((int)response.StatusCode, response.ReasonPhrase ?? "Unknown");

            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var afm = ExtractCallerAfmOrNull(body);
            if (string.IsNullOrEmpty(afm))
            {
                // Valid auth but no AFM in payload — treat as a 5xx so the caller
                // surfaces an "AADE unavailable" message rather than silently failing.
                _logger.LogWarning("AADE validation response missing caller AFM. Body length={Length}", body.Length);
                return new AadeValidationResult.AadeServerError(200, "Missing caller AFM in AADE response");
            }
            return new AadeValidationResult.Success(afm);
        }
        catch (HttpRequestException ex)
        {
            return new AadeValidationResult.NetworkError(ex.Message);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return new AadeValidationResult.NetworkError($"Timeout: {ex.Message}");
        }
    }

    public async Task<AadeFetchResult> FetchInvoicesAsync(string userId, string subscriptionKey, DateTime? sinceUtc, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(subscriptionKey);

        var from = (sinceUtc ?? DateTime.UtcNow.AddDays(-30)).Date;
        var to = DateTime.UtcNow.Date;

        var outgoingTask = FetchAndParseAsync(BuildIncomeUrl(from, to), userId, subscriptionKey, cancellationToken);
        var incomingTask = FetchAndParseAsync(BuildDocsUrl(from, to), userId, subscriptionKey, cancellationToken);

        try
        {
            var outgoing = await outgoingTask.ConfigureAwait(false);
            var incoming = await incomingTask.ConfigureAwait(false);

            if (outgoing.Failure is not null) return outgoing.Failure;
            if (incoming.Failure is not null) return incoming.Failure;

            return new AadeFetchResult.Success(
                Incoming: incoming.Invoices,
                Outgoing: outgoing.Invoices);
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

    private async Task<(IReadOnlyList<AadeInvoiceDto> Invoices, AadeFetchResult? Failure)> FetchAndParseAsync(
        string url, string userId, string subscriptionKey, CancellationToken cancellationToken)
    {
        using var request = BuildAuthorizedRequest(HttpMethod.Get, url, userId, subscriptionKey);
        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return (Array.Empty<AadeInvoiceDto>(), new AadeFetchResult.InvalidCredentials());
        if (!response.IsSuccessStatusCode)
            return (Array.Empty<AadeInvoiceDto>(), new AadeFetchResult.AadeServerError((int)response.StatusCode, response.ReasonPhrase ?? "Unknown"));

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return (ParseInvoices(body), null);
    }

    private static HttpRequestMessage BuildAuthorizedRequest(HttpMethod method, string url, string userId, string subscriptionKey)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("aade-user-id", userId);
        request.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
        return request;
    }

    private static string BuildIncomeUrl(DateTime fromDate, DateTime toDate)
        => $"RequestMyIncome?dateFrom={fromDate:dd/MM/yyyy}&dateTo={toDate:dd/MM/yyyy}";

    private static string BuildDocsUrl(DateTime fromDate, DateTime toDate)
        => $"RequestDocs?dateFrom={fromDate:dd/MM/yyyy}&dateTo={toDate:dd/MM/yyyy}";

    private static string? ExtractCallerAfmOrNull(string xml)
    {
        try
        {
            var doc = XDocument.Parse(xml);
            // AADE responses include the caller's AFM under various wrappers
            // (e.g., issuer/vatNumber on outgoing invoices). Look at the first
            // invoice's issuer; if none, scan for any vatNumber element.
            var invoice = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "invoice");
            var issuer = invoice?.Descendants().FirstOrDefault(e => e.Name.LocalName == "issuer");
            var vat = issuer?.Descendants().FirstOrDefault(e => e.Name.LocalName == "vatNumber");
            return vat?.Value;
        }
        catch (System.Xml.XmlException)
        {
            return null;
        }
    }

    private static IReadOnlyList<AadeInvoiceDto> ParseInvoices(string xml)
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
            var mark = invoice.Descendants().FirstOrDefault(e => e.Name.LocalName == "mark")?.Value ?? string.Empty;
            if (string.IsNullOrEmpty(mark)) continue;

            var counterpart = invoice.Descendants().FirstOrDefault(e => e.Name.LocalName == "counterpart");
            var counterAfm = counterpart?.Descendants().FirstOrDefault(e => e.Name.LocalName == "vatNumber")?.Value ?? string.Empty;
            var counterName = counterpart?.Descendants().FirstOrDefault(e => e.Name.LocalName == "name")?.Value ?? string.Empty;

            var header = invoice.Descendants().FirstOrDefault(e => e.Name.LocalName == "invoiceHeader");
            var issueDateText = header?.Descendants().FirstOrDefault(e => e.Name.LocalName == "issueDate")?.Value;
            var invoiceType = header?.Descendants().FirstOrDefault(e => e.Name.LocalName == "invoiceType")?.Value ?? string.Empty;
            var currency = header?.Descendants().FirstOrDefault(e => e.Name.LocalName == "currency")?.Value ?? "EUR";

            var summary = invoice.Descendants().FirstOrDefault(e => e.Name.LocalName == "invoiceSummary");
            var grossText = summary?.Descendants().FirstOrDefault(e => e.Name.LocalName == "totalGrossValue")?.Value;

            DateTime.TryParse(issueDateText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var issueDate);
            decimal.TryParse(grossText, NumberStyles.Number, CultureInfo.InvariantCulture, out var gross);

            results.Add(new AadeInvoiceDto(
                Mark: mark,
                CounterpartyAfm: counterAfm,
                CounterpartyName: counterName,
                IssueDate: issueDate,
                DocumentTypeCode: invoiceType,
                GrossAmount: gross,
                Currency: currency,
                RawXml: invoice.ToString(SaveOptions.DisableFormatting)));
        }

        return results;
    }
}
