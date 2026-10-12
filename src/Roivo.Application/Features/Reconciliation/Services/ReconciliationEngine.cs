using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Features.Reconciliation.Services;

/// <summary>
/// Scores invoice/transaction pairs on amount, date proximity and counterparty
/// similarity, then assigns each invoice its best remaining transaction.
/// </summary>
public sealed class ReconciliationEngine : IReconciliationEngine
{
    // Weights sum to 1.0 at a perfect match: amount 0.4 + date 0.3 + party 0.3.
    private const decimal ExactAmountScore = 0.4m;
    private const decimal ToleranceAmountScore = 0.2m;
    private const decimal DateWithinOneDayScore = 0.3m;
    private const decimal DateWithinThreeDaysScore = 0.2m;
    private const decimal DateWithinSevenDaysScore = 0.1m;
    private const decimal CounterpartyScore = 0.3m;

    /// <summary>Similarity at or above which two counterparty names are treated as the same party.</summary>
    private const double CounterpartySimilarityThreshold = 0.75;

    private readonly IReconciliationRepository _repository;

    public ReconciliationEngine(IReconciliationRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public async Task<ReconciliationResult> ReconcileAsync(
        Guid businessId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var invoices = await _repository
            .ListUnreconciledInvoicesAsync(businessId, from, to, cancellationToken)
            .ConfigureAwait(false);

        var transactions = await _repository
            .ListUnreconciledTransactionsAsync(businessId, from, to, cancellationToken)
            .ConfigureAwait(false);

        if (invoices.Count == 0 && transactions.Count == 0)
            return ReconciliationResult.Empty;

        var rule = await _repository.GetActiveRuleAsync(businessId, cancellationToken).ConfigureAwait(false);
        var rejected = await _repository.ListRejectedPairsAsync(businessId, cancellationToken).ConfigureAwait(false);
        var rejectedPairs = rejected.ToHashSet();

        var amountTolerancePercent = rule?.AmountTolerancePercent ?? ReconciliationRule.Defaults.AmountTolerancePercent;
        var dateToleranceDays = rule?.DateToleranceDays ?? ReconciliationRule.Defaults.DateToleranceDays;

        // Score every surviving pair once, then take them best-first. Greedy
        // assignment over a globally sorted list means a strong pair claims its
        // transaction before a weaker pair competing for the same one — without
        // the cost of a full optimal assignment, which this data does not justify.
        var candidates = new List<Candidate>();
        foreach (var invoice in invoices)
        {
            foreach (var transaction in transactions)
            {
                if (rejectedPairs.Contains((invoice.Id, transaction.Id)))
                    continue;

                if (!DirectionAgrees(invoice, transaction))
                    continue;

                var score = Score(invoice, transaction, amountTolerancePercent, dateToleranceDays, rule?.CounterpartyPattern);
                if (score >= ReconciliationMatch.SuggestedThreshold)
                    candidates.Add(new Candidate(invoice, transaction, score));
            }
        }

        var autoMatched = new List<ReconciliationMatch>();
        var suggested = new List<ReconciliationMatch>();
        var claimedInvoices = new HashSet<Guid>();
        var claimedTransactions = new HashSet<Guid>();

        foreach (var candidate in candidates.OrderByDescending(c => c.Score))
        {
            if (!claimedInvoices.Add(candidate.Invoice.Id))
                continue;

            if (!claimedTransactions.Add(candidate.Transaction.Id))
            {
                // The invoice was just claimed above but its transaction is gone;
                // release it so a weaker pair can still match it.
                claimedInvoices.Remove(candidate.Invoice.Id);
                continue;
            }

            var match = ReconciliationMatch.CreateFromEngine(
                businessId,
                candidate.Invoice.Id,
                candidate.Transaction.Id,
                candidate.Score);

            if (match.MatchType == ReconciliationMatchType.Automatic)
                autoMatched.Add(match);
            else
                suggested.Add(match);
        }

        var unmatchedInvoices = invoices.Where(i => !claimedInvoices.Contains(i.Id)).ToList();
        var unmatchedTransactions = transactions.Where(t => !claimedTransactions.Contains(t.Id)).ToList();

        var reconciledAmount = autoMatched
            .Join(invoices, m => m.InvoiceId, i => i.Id, (_, i) => i.GrossAmount)
            .Sum();

        var summary = new ReconciliationSummary(
            TotalInvoices: invoices.Count,
            TotalTransactions: transactions.Count,
            AutoMatchedCount: autoMatched.Count,
            SuggestedCount: suggested.Count,
            UnmatchedInvoiceCount: unmatchedInvoices.Count,
            UnmatchedTransactionCount: unmatchedTransactions.Count,
            AutoMatchRate: invoices.Count == 0
                ? 0m
                : Math.Round((decimal)autoMatched.Count / invoices.Count, 4),
            TotalReconciledAmount: reconciledAmount);

        return new ReconciliationResult(autoMatched, suggested, unmatchedInvoices, unmatchedTransactions, summary);
    }

    /// <summary>
    /// Money has to flow the right way: an invoice we issued is settled by money
    /// coming in, one we received by money going out. The client normalises
    /// Enable Banking's CRDT/DBIT indicator into the sign, so a credit is
    /// positive. Without this check a refund of the same size scores
    /// identically to the payment.
    /// </summary>
    private static bool DirectionAgrees(Invoice invoice, BankTransaction transaction)
        => invoice.Direction == InvoiceDirection.Issued ? transaction.Amount > 0m : transaction.Amount < 0m;

    internal static decimal Score(
        Invoice invoice,
        BankTransaction transaction,
        decimal amountTolerancePercent,
        int dateToleranceDays,
        string? counterpartyPattern)
    {
        var score = 0m;

        var invoiceAmount = Math.Abs(invoice.GrossAmount);
        var transactionAmount = Math.Abs(transaction.Amount);
        var difference = Math.Abs(invoiceAmount - transactionAmount);

        if (difference == 0m)
        {
            score += ExactAmountScore;
        }
        else if (invoiceAmount > 0m)
        {
            var differencePercent = difference / invoiceAmount * 100m;
            if (differencePercent <= amountTolerancePercent)
                score += ToleranceAmountScore;
            else
                return 0m; // Outside tolerance the pair is not the same money, whatever else lines up.
        }
        else
        {
            return 0m;
        }

        var dayGap = Math.Abs(transaction.BookingDate.DayNumber - invoice.IssueDate.DayNumber);
        var effectiveDateWindow = Math.Max(dateToleranceDays, 7);

        if (dayGap <= 1)
            score += DateWithinOneDayScore;
        else if (dayGap <= Math.Max(dateToleranceDays, 3))
            score += DateWithinThreeDaysScore;
        else if (dayGap <= effectiveDateWindow)
            score += DateWithinSevenDaysScore;

        if (CounterpartyMatches(invoice, transaction, counterpartyPattern))
            score += CounterpartyScore;

        return Math.Min(score, 1.0m);
    }

    private static bool CounterpartyMatches(Invoice invoice, BankTransaction transaction, string? counterpartyPattern)
    {
        var haystack = $"{transaction.CounterpartyName} {transaction.Reference}";

        // A business-defined pattern is an explicit statement about how this
        // bank labels this counterparty, so it outranks fuzzy name comparison.
        if (!string.IsNullOrWhiteSpace(counterpartyPattern)
            && haystack.Contains(counterpartyPattern, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Greek banks frequently put the payer's AFM in the reference line.
        if (!string.IsNullOrWhiteSpace(invoice.CounterpartyAfm)
            && haystack.Contains(invoice.CounterpartyAfm, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(invoice.CounterpartyName)
            || string.IsNullOrWhiteSpace(transaction.CounterpartyName))
        {
            return false;
        }

        return Similarity(
            Normalize(invoice.CounterpartyName),
            Normalize(transaction.CounterpartyName)) >= CounterpartySimilarityThreshold;
    }

    /// <summary>
    /// Folds case and strips punctuation and company-form suffixes, which banks
    /// and AADE spell differently for the same legal entity.
    /// </summary>
    private static string Normalize(string value)
    {
        var cleaned = new string(value
            .Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
            .ToArray())
            .ToUpperInvariant();

        foreach (var suffix in CompanyFormSuffixes)
            cleaned = cleaned.Replace(suffix, " ", StringComparison.Ordinal);

        return string.Join(' ', cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static readonly string[] CompanyFormSuffixes =
    [
        " AE", " ΑΕ", " EPE", " ΕΠΕ", " IKE", " ΙΚΕ", " OE", " ΟΕ", " EE", " ΕΕ", " SA", " LTD",
    ];

    /// <summary>Levenshtein distance normalised to a [0,1] similarity.</summary>
    private static double Similarity(string left, string right)
    {
        if (left.Length == 0 && right.Length == 0)
            return 1.0;
        if (left.Length == 0 || right.Length == 0)
            return 0.0;
        if (string.Equals(left, right, StringComparison.Ordinal))
            return 1.0;

        var distance = LevenshteinDistance(left, right);
        return 1.0 - (double)distance / Math.Max(left.Length, right.Length);
    }

    /// <summary>
    /// Two-row Levenshtein. Names are short, but this runs once per candidate
    /// pair across a 90-day window, so the full matrix is not worth allocating.
    /// </summary>
    private static int LevenshteinDistance(string left, string right)
    {
        var previous = new int[right.Length + 1];
        var current = new int[right.Length + 1];

        for (var j = 0; j <= right.Length; j++)
            previous[j] = j;

        for (var i = 1; i <= left.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= right.Length; j++)
            {
                var substitutionCost = left[i - 1] == right[j - 1] ? 0 : 1;
                current[j] = Math.Min(
                    Math.Min(current[j - 1] + 1, previous[j] + 1),
                    previous[j - 1] + substitutionCost);
            }

            (previous, current) = (current, previous);
        }

        return previous[right.Length];
    }

    private sealed record Candidate(Invoice Invoice, BankTransaction Transaction, decimal Score);
}
