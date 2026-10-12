using Microsoft.EntityFrameworkCore;
using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Enums;
using Roivo.Infrastructure.Persistence;

namespace Roivo.Infrastructure.Entitlements;

/// <inheritdoc />
public sealed class EntitlementService : IEntitlementService
{
    /// <summary>
    /// The tier each capability needs. One table, so re-pricing is a change
    /// here rather than a hunt through the pages that call it.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, PlanTier> Gates =
        new Dictionary<string, PlanTier>(StringComparer.Ordinal)
        {
            [Features.ForecastDays60] = PlanTier.Pro,
            [Features.ForecastDays90] = PlanTier.Premium,
            [Features.DashboardCustomizationSave] = PlanTier.Pro,
            [Features.CustomReportRange] = PlanTier.Pro,
            [Features.CalendarNotes] = PlanTier.Pro,
        };

    private readonly IDbContextFactory<ApplicationDbContext> _factory;
    private readonly ITenantContext _tenant;

    public EntitlementService(
        IDbContextFactory<ApplicationDbContext> factory,
        ITenantContext tenant)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(tenant);

        _factory = factory;
        _tenant = tenant;
    }

    public async Task<PlanTier> GetCurrentPlanAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenant.CurrentTenantId;

        // No tenant in scope is an anonymous or background caller. Starter is
        // the safe answer: it grants nothing.
        if (tenantId == Guid.Empty) return PlanTier.Starter;

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        return await db.Tenants
            .AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => t.PlanTier)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> CanAccessFeatureAsync(
        string featureKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureKey);

        // A key nobody gated is a feature everybody has. Forgetting to add a
        // gate should leave a feature working, not lock it for every customer.
        if (!Gates.TryGetValue(featureKey, out var required)) return true;

        return await GetCurrentPlanAsync(cancellationToken) >= required;
    }
}
