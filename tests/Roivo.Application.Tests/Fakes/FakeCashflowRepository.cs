using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Tests.Fakes;

/// <summary>In-memory stand-in for <see cref="ICashflowRepository"/>.</summary>
public sealed class FakeCashflowRepository : ICashflowRepository
{
    public List<DailyCashMovement> Movements { get; } = [];
    public List<CashflowCategory> Categories { get; } = [];
    public List<TaxObligation> TaxObligations { get; } = [];
    public List<CashflowForecast> StoredForecasts { get; } = [];
    public decimal CurrentBalance { get; set; }
    public VatTotals VatTotals { get; set; } = new(0m, 0m);
    public decimal NetRevenue { get; set; }

    public Task<IReadOnlyList<DailyCashMovement>> ListDailyMovementsAsync(
        Guid businessId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<DailyCashMovement>>(
            [.. Movements.Where(m => m.Date >= from && m.Date <= to).OrderBy(m => m.Date)]);

    public Task<decimal> GetCurrentBalanceAsync(Guid businessId, CancellationToken cancellationToken = default)
        => Task.FromResult(CurrentBalance);

    public Task<DateOnly?> GetEarliestTransactionDateAsync(
        Guid businessId, CancellationToken cancellationToken = default)
        => Task.FromResult(Movements.Count == 0 ? null : (DateOnly?)Movements.Min(m => m.Date));

    public Task<int> CountTransactionsAsync(Guid businessId, CancellationToken cancellationToken = default)
        => Task.FromResult(Movements.Count);

    public Task<IReadOnlyList<CashflowCategory>> ListCategoriesAsync(
        Guid businessId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<CashflowCategory>>(
            [.. Categories.Where(c => c.BusinessId == businessId)]);

    public Task<CashflowCategory> AddCategoryAsync(
        CashflowCategory category, CancellationToken cancellationToken = default)
    {
        Categories.Add(category);
        return Task.FromResult(category);
    }

    public Task<IReadOnlyList<TaxObligation>> ListTaxObligationsAsync(
        Guid businessId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<TaxObligation>>(
            [.. TaxObligations.Where(t => t.BusinessId == businessId && t.DueDate >= from && t.DueDate <= to)]);

    public Task<TaxObligation?> GetTaxObligationByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(TaxObligations.FirstOrDefault(t => t.Id == id));

    public Task UpdateTaxObligationAsync(TaxObligation obligation, CancellationToken cancellationToken = default)
    {
        if (!TaxObligations.Contains(obligation))
            TaxObligations.Add(obligation);
        return Task.CompletedTask;
    }

    public Task<int> UpsertTaxObligationsAsync(
        IReadOnlyCollection<TaxObligation> obligations, CancellationToken cancellationToken = default)
    {
        var inserted = 0;
        foreach (var obligation in obligations)
        {
            var exists = TaxObligations.Any(t => t.BusinessId == obligation.BusinessId
                && t.TaxType == obligation.TaxType
                && t.Period == obligation.Period);

            if (exists)
                continue;

            TaxObligations.Add(obligation);
            inserted++;
        }

        return Task.FromResult(inserted);
    }

    public Task ReplaceForecastsAsync(
        Guid businessId, IReadOnlyCollection<CashflowForecast> forecasts,
        CancellationToken cancellationToken = default)
    {
        StoredForecasts.RemoveAll(f => f.BusinessId == businessId);
        StoredForecasts.AddRange(forecasts);
        return Task.CompletedTask;
    }

    public Task<VatTotals> GetVatTotalsAsync(
        Guid businessId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
        => Task.FromResult(VatTotals);

    public Task<decimal> GetNetRevenueAsync(
        Guid businessId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
        => Task.FromResult(NetRevenue);
}
