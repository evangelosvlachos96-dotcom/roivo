using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Abstractions;

/// <summary>
/// Persistence boundary for reconciliation. Same stateless-factory pattern as
/// <see cref="IBusinessRepository"/>: every method opens and disposes its own
/// context, and returned entities are detached.
/// </summary>
public interface IReconciliationRepository
{
    /// <summary>
    /// Invoices for the business issued within the window that no confirmed
    /// match accounts for yet.
    /// </summary>
    Task<IReadOnlyList<Invoice>> ListUnreconciledInvoicesAsync(
        Guid businessId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Transactions across every bank account of the business, booked within the
    /// window, that no confirmed match accounts for yet.
    /// </summary>
    Task<IReadOnlyList<BankTransaction>> ListUnreconciledTransactionsAsync(
        Guid businessId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invoice/transaction pairs a user already rejected. The engine must not
    /// re-propose these — a rejection is a decision, not a one-off dismissal.
    /// </summary>
    Task<IReadOnlyList<(Guid InvoiceId, Guid BankTransactionId)>> ListRejectedPairsAsync(
        Guid businessId,
        CancellationToken cancellationToken = default);

    /// <summary>The business's active matching rule, or null to use the defaults.</summary>
    Task<ReconciliationRule?> GetActiveRuleAsync(
        Guid businessId,
        CancellationToken cancellationToken = default);

    Task<ReconciliationMatch?> GetMatchByIdAsync(Guid matchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists new matches and applies the reconciled flag to both sides of
    /// every confirmed one, in a single transaction. Pairs that already have a
    /// non-rejected match are skipped rather than duplicated.
    /// </summary>
    /// <returns>How many matches were actually inserted.</returns>
    Task<int> AddMatchesAsync(
        IReadOnlyCollection<ReconciliationMatch> matches,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a changed match and brings the reconciled flags on its invoice and
    /// transaction into line with its status.
    /// </summary>
    Task UpdateMatchAsync(ReconciliationMatch match, CancellationToken cancellationToken = default);

    /// <summary>True when a non-rejected match already ties this pair together.</summary>
    Task<bool> PairIsMatchedAsync(
        Guid invoiceId,
        Guid bankTransactionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// True when either side already belongs to a confirmed match. Guards manual
    /// matching against double-counting the same money.
    /// </summary>
    Task<bool> EitherSideIsReconciledAsync(
        Guid invoiceId,
        Guid bankTransactionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Matches with both sides already joined. Returning the detail in one query
    /// keeps the history and dashboard views off an N+1 path.
    /// </summary>
    Task<IReadOnlyList<ReconciliationMatchDetail>> ListMatchDetailsAsync(
        Guid businessId,
        ReconciliationMatchStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<int> CountMatchesAsync(
        Guid businessId,
        ReconciliationMatchStatus? status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The same counts as <see cref="GetCountsAsync"/> for many businesses at
    /// once, in a fixed number of round-trips rather than one set per business.
    /// </summary>
    /// <remarks>
    /// The accountant pages render a row per client, and calling the single
    /// version in that loop made the query count scale with the size of the
    /// client book. Businesses with nothing in the window are present in the
    /// result with zeroed counts, so callers never have to distinguish "no
    /// rows" from "not asked for".
    /// </remarks>
    Task<IReadOnlyDictionary<Guid, ReconciliationCounts>> GetCountsForBusinessesAsync(
        IReadOnlyCollection<Guid> businessIds,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    Task<ReconciliationCounts> GetCountsAsync(
        Guid businessId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Invoice>> ListUnreconciledInvoicesPagedAsync(
        Guid businessId, int skip, int take, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BankTransaction>> ListUnreconciledTransactionsPagedAsync(
        Guid businessId, int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads an invoice and a transaction for manual matching, confirming both
    /// belong to the given business. Either may come back null.
    /// </summary>
    Task<(Invoice? Invoice, BankTransaction? Transaction)> GetPairForBusinessAsync(
        Guid businessId,
        Guid invoiceId,
        Guid bankTransactionId,
        CancellationToken cancellationToken = default);
}

/// <summary>Aggregate counts behind the reconciliation dashboard.</summary>
public sealed record ReconciliationCounts(
    int TotalInvoices,
    int TotalTransactions,
    int ConfirmedMatches,
    int PendingMatches,
    int UnreconciledInvoices,
    int UnreconciledTransactions,
    decimal ReconciledAmount)
{
    /// <summary>A business with nothing in the requested window.</summary>
    public static ReconciliationCounts Empty { get; } = new(0, 0, 0, 0, 0, 0, 0m);
}

/// <summary>A match with the invoice and transaction fields a list view needs.</summary>
public sealed record ReconciliationMatchDetail(
    Guid MatchId,
    Guid InvoiceId,
    Guid BankTransactionId,
    ReconciliationMatchType MatchType,
    ReconciliationMatchStatus Status,
    decimal Confidence,
    DateTime MatchedAt,
    DateOnly InvoiceIssueDate,
    decimal InvoiceGrossAmount,
    string? InvoiceCounterpartyName,
    DateOnly TransactionBookingDate,
    decimal TransactionAmount,
    string? TransactionCounterpartyName);
