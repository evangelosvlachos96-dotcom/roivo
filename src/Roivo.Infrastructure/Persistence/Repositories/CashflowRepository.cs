using Microsoft.EntityFrameworkCore;
using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Enums;

namespace Roivo.Infrastructure.Persistence.Repositories;

public sealed class CashflowRepository : ICashflowRepository
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public CashflowRepository(IDbContextFactory<ApplicationDbContext> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    public async Task<IReadOnlyList<DailyCashMovement>> ListDailyMovementsAsync(
        Guid businessId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        var rows = await TransactionsFor(db, businessId)
            .Where(t => t.BookingDate >= from && t.BookingDate <= to)
            .GroupBy(t => t.BookingDate)
            .Select(g => new
            {
                Date = g.Key,
                Inflow = g.Where(t => t.Amount > 0m).Sum(t => (decimal?)t.Amount) ?? 0m,
                Outflow = g.Where(t => t.Amount < 0m).Sum(t => (decimal?)t.Amount) ?? 0m,
            })
            .ToListAsync(cancellationToken);

        // Outflow is stored negative; the engine works in unsigned magnitudes.
        return [.. rows
            .Select(r => new DailyCashMovement(r.Date, r.Inflow, Math.Abs(r.Outflow)))
            .OrderBy(r => r.Date)];
    }

    public async Task<decimal> GetCurrentBalanceAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        return await db.BankAccounts
            .IgnoreQueryFilters()
            .Where(a => a.BusinessId == businessId)
            .SumAsync(a => (decimal?)a.CurrentBalance, cancellationToken) ?? 0m;
    }

    public async Task<DateOnly?> GetEarliestTransactionDateAsync(
        Guid businessId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        var dates = TransactionsFor(db, businessId).Select(t => (DateOnly?)t.BookingDate);
        return await dates.MinAsync(cancellationToken);
    }

    public async Task<int> CountTransactionsAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await TransactionsFor(db, businessId).CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CashflowCategory>> ListCategoriesAsync(
        Guid businessId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        return await db.CashflowCategories
            .IgnoreQueryFilters()
            .Where(c => c.BusinessId == businessId)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<CashflowCategory> AddCategoryAsync(
        CashflowCategory category, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(category);

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        db.CashflowCategories.Add(category);
        await db.SaveChangesAsync(cancellationToken);
        return category;
    }

    public async Task<IReadOnlyList<TaxObligation>> ListTaxObligationsAsync(
        Guid businessId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        return await db.TaxObligations
            .IgnoreQueryFilters()
            .Where(t => t.BusinessId == businessId && t.DueDate >= from && t.DueDate <= to)
            .OrderBy(t => t.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<TaxObligation?> GetTaxObligationByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.TaxObligations.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task UpdateTaxObligationAsync(
        TaxObligation obligation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(obligation);

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        db.TaxObligations.Update(obligation);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> UpsertTaxObligationsAsync(
        IReadOnlyCollection<TaxObligation> obligations, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(obligations);

        if (obligations.Count == 0)
            return 0;

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        var businessIds = obligations.Select(o => o.BusinessId).Distinct().ToList();
        var periods = obligations.Select(o => o.Period).Distinct().ToList();

        var existing = await db.TaxObligations
            .IgnoreQueryFilters()
            .Where(t => businessIds.Contains(t.BusinessId) && periods.Contains(t.Period))
            .ToListAsync(cancellationToken);

        var existingKeys = existing
            .Select(t => (t.BusinessId, t.TaxType, t.Period))
            .ToHashSet();

        var inserted = 0;
        foreach (var obligation in obligations)
        {
            var key = (obligation.BusinessId, obligation.TaxType, obligation.Period);

            // Only new periods are inserted. An obligation already on file may
            // carry a recorded payment, and regenerating the calendar must never
            // overwrite that.
            if (!existingKeys.Add(key))
                continue;

            db.TaxObligations.Add(obligation);
            inserted++;
        }

        if (inserted > 0)
            await db.SaveChangesAsync(cancellationToken);

        return inserted;
    }

    public async Task ReplaceForecastsAsync(
        Guid businessId,
        IReadOnlyCollection<CashflowForecast> forecasts,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(forecasts);

        if (forecasts.Count == 0)
            return;

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        var from = forecasts.Min(f => f.ForecastDate);
        var to = forecasts.Max(f => f.ForecastDate);

        // Delete-then-insert over the covered window rather than upserting row
        // by row: a run supersedes the previous projection wholesale, and the
        // unique (business, date) index would otherwise reject the overlap.
        var stale = await db.CashflowForecasts
            .IgnoreQueryFilters()
            .Where(f => f.BusinessId == businessId && f.ForecastDate >= from && f.ForecastDate <= to)
            .ToListAsync(cancellationToken);

        db.CashflowForecasts.RemoveRange(stale);
        db.CashflowForecasts.AddRange(forecasts);

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CashflowForecast>> ListStoredForecastsAsync(
        IReadOnlyCollection<Guid> businessIds, DateOnly from, DateOnly to,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(businessIds);

        if (businessIds.Count == 0)
            return [];

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        var ids = businessIds.ToList();

        return await db.CashflowForecasts
            .IgnoreQueryFilters()
            .Where(f => ids.Contains(f.BusinessId) && f.ForecastDate >= from && f.ForecastDate <= to)
            .OrderBy(f => f.BusinessId).ThenBy(f => f.ForecastDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaxObligation>> ListTaxObligationsForBusinessesAsync(
        IReadOnlyCollection<Guid> businessIds, DateOnly from, DateOnly to,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(businessIds);

        if (businessIds.Count == 0)
            return [];

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        var ids = businessIds.ToList();

        return await db.TaxObligations
            .IgnoreQueryFilters()
            .Where(t => ids.Contains(t.BusinessId) && t.DueDate >= from && t.DueDate <= to)
            .OrderBy(t => t.DueDate).ThenBy(t => t.TaxType)
            .ToListAsync(cancellationToken);
    }

    public async Task<VatTotals> GetVatTotalsAsync(
        Guid businessId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        var rows = await db.Invoices
            .IgnoreQueryFilters()
            .Where(i => i.BusinessId == businessId
                && i.IssueDate >= from && i.IssueDate <= to
                && i.CancelledByMark == null)
            .GroupBy(i => i.Direction)
            .Select(g => new { Direction = g.Key, Vat = g.Sum(i => (decimal?)i.VatAmount) ?? 0m })
            .ToListAsync(cancellationToken);

        var output = rows.FirstOrDefault(r => r.Direction == InvoiceDirection.Issued)?.Vat ?? 0m;
        var input = rows.FirstOrDefault(r => r.Direction == InvoiceDirection.Received)?.Vat ?? 0m;

        return new VatTotals(output, input);
    }

    public async Task<decimal> GetNetRevenueAsync(
        Guid businessId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        return await db.Invoices
            .IgnoreQueryFilters()
            .Where(i => i.BusinessId == businessId
                && i.Direction == InvoiceDirection.Issued
                && i.IssueDate >= from && i.IssueDate <= to
                && i.CancelledByMark == null)
            .SumAsync(i => (decimal?)i.NetAmount, cancellationToken) ?? 0m;
    }

    public async Task<BusinessTaxProfile?> GetTaxProfileAsync(
        Guid businessId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        return await db.Businesses
            .IgnoreQueryFilters()
            .Where(b => b.Id == businessId)
            .Select(b => new BusinessTaxProfile(b.VatFrequency, b.EstimatedPropertyValue))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static IQueryable<BankTransaction> TransactionsFor(ApplicationDbContext db, Guid businessId)
        => db.BankTransactions
            .IgnoreQueryFilters()
            .Join(db.BankAccounts.IgnoreQueryFilters(), t => t.BankAccountId, a => a.Id,
                (t, a) => new { Transaction = t, a.BusinessId })
            .Where(x => x.BusinessId == businessId)
            .Select(x => x.Transaction);
}
