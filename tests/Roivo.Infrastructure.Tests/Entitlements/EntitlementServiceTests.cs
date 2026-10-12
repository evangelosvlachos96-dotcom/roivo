using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Enums;
using Roivo.Infrastructure.Entitlements;
using Roivo.Infrastructure.Persistence;

namespace Roivo.Infrastructure.Tests.Entitlements;

/// <summary>
/// Covers the gate itself: that a tier grants everything at or below it, that
/// an ungated capability stays open, and that a caller with no tenant gets
/// nothing.
/// </summary>
public sealed class EntitlementServiceTests : IDisposable
{
    private static CancellationToken TestCancellation => CancellationToken.None;

    private readonly ServiceProvider _services;
    private readonly IDbContextFactory<ApplicationDbContext> _factory;
    private readonly StubTenantContext _tenant = new();

    public EntitlementServiceTests()
    {
        _services = new ServiceCollection()
            .AddSingleton<ITenantContext>(_tenant)
            .AddDbContextFactory<ApplicationDbContext>(
                options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()))
            .BuildServiceProvider();

        _factory = _services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
    }

    private async Task<EntitlementService> OnPlanAsync(PlanTier tier)
    {
        await using var db = await _factory.CreateDbContextAsync(TestCancellation);

        db.Tenants.Add(new Tenant
        {
            Id = _tenant.CurrentTenantId,
            Name = "Acme",
            Afm = "094014201",
            Type = TenantType.Business,
            PlanTier = tier,
        });

        await db.SaveChangesAsync(TestCancellation);

        return new EntitlementService(_factory, _tenant);
    }

    [Fact]
    public async Task ANewTenantIsOnStarter()
    {
        var sut = await OnPlanAsync(PlanTier.Starter);

        (await sut.GetCurrentPlanAsync(TestCancellation)).Should().Be(PlanTier.Starter);
    }

    [Theory]
    [InlineData(Features.ForecastDays60)]
    [InlineData(Features.ForecastDays90)]
    [InlineData(Features.DashboardCustomizationSave)]
    [InlineData(Features.CustomReportRange)]
    [InlineData(Features.CalendarNotes)]
    public async Task StarterIsRefusedEveryPaidFeature(string feature)
    {
        var sut = await OnPlanAsync(PlanTier.Starter);

        (await sut.CanAccessFeatureAsync(feature, TestCancellation)).Should().BeFalse();
    }

    [Fact]
    public async Task ProGetsTheProFeaturesButNotPremium()
    {
        var sut = await OnPlanAsync(PlanTier.Pro);

        (await sut.CanAccessFeatureAsync(Features.ForecastDays60, TestCancellation)).Should().BeTrue();
        (await sut.CanAccessFeatureAsync(Features.CustomReportRange, TestCancellation)).Should().BeTrue();
        (await sut.CanAccessFeatureAsync(Features.ForecastDays90, TestCancellation)).Should().BeFalse();
    }

    [Fact]
    public async Task PremiumGetsEverythingProDoes()
    {
        var sut = await OnPlanAsync(PlanTier.Premium);

        // The check is a >= against the required tier, so the higher tier must
        // carry the lower tier's features without being listed again.
        (await sut.CanAccessFeatureAsync(Features.ForecastDays60, TestCancellation)).Should().BeTrue();
        (await sut.CanAccessFeatureAsync(Features.ForecastDays90, TestCancellation)).Should().BeTrue();
    }

    [Fact]
    public async Task AFeatureNobodyGatedStaysOpen()
    {
        var sut = await OnPlanAsync(PlanTier.Starter);

        // Forgetting to add a gate should leave a feature working rather than
        // silently locking it for every customer.
        (await sut.CanAccessFeatureAsync("something.ungated", TestCancellation)).Should().BeTrue();
    }

    [Fact]
    public async Task ACallerWithNoTenantGetsStarter()
    {
        var sut = await OnPlanAsync(PlanTier.Premium);
        _tenant.Clear();

        (await sut.GetCurrentPlanAsync(TestCancellation)).Should().Be(PlanTier.Starter);
        (await sut.CanAccessFeatureAsync(Features.ForecastDays90, TestCancellation)).Should().BeFalse();
    }

    [Fact]
    public async Task AnotherTenantsPlanIsNotBorrowed()
    {
        var sut = await OnPlanAsync(PlanTier.Starter);

        await using (var db = await _factory.CreateDbContextAsync(TestCancellation))
        {
            db.Tenants.Add(new Tenant
            {
                Id = Guid.NewGuid(),
                Name = "Someone else",
                Afm = "090000045",
                Type = TenantType.Business,
                PlanTier = PlanTier.Premium,
            });

            await db.SaveChangesAsync(TestCancellation);
        }

        (await sut.GetCurrentPlanAsync(TestCancellation)).Should().Be(PlanTier.Starter);
    }

    public void Dispose() => _services.Dispose();

    private sealed class StubTenantContext : ITenantContext
    {
        private Guid _id = Guid.NewGuid();

        public Guid CurrentTenantId => _id;
        public TenantType CurrentTenantType => TenantType.Business;

        /// <summary>Stands in for an anonymous or background caller.</summary>
        public void Clear() => _id = Guid.Empty;
    }
}
