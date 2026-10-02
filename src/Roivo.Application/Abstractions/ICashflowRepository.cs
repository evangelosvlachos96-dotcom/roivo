using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Abstractions;

/// <summary>
/// Persistence boundary for cashflow forecasting and the tax calendar. Same
/// stateless-factory pattern as <see cref="IBusinessRepository"/>.
/// </summary>
public interface ICashflowRepository
{
    /// <summary>
    /// Daily net movements over the lookback window, derived from bank
    /// transactions. Inflow and outflow are reported separately because their
    /// patterns differ: receipts are lumpy, costs are regular.
    /// </summary>
    Task<IReadOnlyList<DailyCashMovement>> ListDailyMovementsAsync(
        Guid businessId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    /// <summary>Sum of current balances across the business's bank accounts.</summary>
    Task<decimal> GetCurrentBalanceAsync(Guid businessId, CancellationToken cancellationToken = default);

    /// <summary>Date of the earliest transaction, or null when the business has none.</summary>
    Task<DateOnly?> GetEarliestTransactionDateAsync(Guid businessId, CancellationToken cancellationToken = default);

    Task<int> CountTransactionsAsync(Guid businessId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CashflowCategory>> ListCategoriesAsync(
        Guid businessId, CancellationToken cancellationToken = default);

    Task<CashflowCategory> AddCategoryAsync(
        CashflowCategory category, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaxObligation>> ListTaxObligationsAsync(
        Guid businessId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    Task<TaxObligation?> GetTaxObligationByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task UpdateTaxObligationAsync(TaxObligation obligation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts obligations the business does not already have, matching on
    /// (business, tax type, period). Regenerating the calendar is therefore
    /// idempotent and never clobbers a payment somebody already recorded.
    /// </summary>
    Task<int> UpsertTaxObligationsAsync(
        IReadOnlyCollection<TaxObligation> obligations,
        CancellationToken cancellationToken = default);

    /// <summary>Replaces any stored forecast for the covered dates with this run.</summary>
    Task ReplaceForecastsAsync(
        Guid businessId,
        IReadOnlyCollection<CashflowForecast> forecasts,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Total VAT on invoices issued in the window. Feeds the VAT estimate, which
    /// is output tax less input tax.
    /// </summary>
    Task<VatTotals> GetVatTotalsAsync(
        Guid businessId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    /// <summary>Net revenue from issued invoices in the window, for the income-tax estimate.</summary>
    Task<decimal> GetNetRevenueAsync(
        Guid businessId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);
}

/// <summary>One day of realised cash movement. Both figures are unsigned magnitudes.</summary>
public sealed record DailyCashMovement(DateOnly Date, decimal Inflow, decimal Outflow);

/// <summary>VAT charged on sales versus VAT paid on purchases over a period.</summary>
public sealed record VatTotals(decimal OutputVat, decimal InputVat)
{
    /// <summary>What is payable: output less input, floored at zero (a credit carries forward).</summary>
    public decimal Payable => Math.Max(0m, OutputVat - InputVat);
}
