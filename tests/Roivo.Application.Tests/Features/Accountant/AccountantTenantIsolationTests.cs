using FluentAssertions;
using Roivo.Application.Abstractions;
using Roivo.Application.Features.Accountant.Queries.GetAccountantAlerts;
using Roivo.Application.Features.Accountant.Queries.GetAccountantDashboard;
using Roivo.Application.Features.Accountant.Queries.GetAccountantReport;
using Roivo.Application.Tests.Fakes;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Tests.Features.Accountant;

/// <summary>
/// The accountant workspace is a cross-business view, which makes it the one
/// place where reaching for the wrong repository method silently exposes
/// another accountant's client book. These tests put the tenant filter the real
/// repository inherits from EF's global query filter in front of the fake, so a
/// handler that called an <c>...AcrossAllTenantsAsync</c> read instead of
/// <see cref="IBusinessRepository.ListActiveAsync"/> fails here rather than in
/// production.
/// </summary>
public class AccountantTenantIsolationTests
{
    private const string MineAfm = "094014201";
    private const string TheirsAfm = "123456783";

    private static readonly DateOnly Today = new(2026, 6, 15);

    private sealed record Sut(
        GetAccountantDashboardHandler Dashboard,
        GetAccountantAlertsHandler Alerts,
        GetAccountantReportHandler Report,
        TenantScopedBusinessRepository Businesses,
        FakeTenantContext Tenant);

    private static Sut BuildSut()
    {
        var tenant = new FakeTenantContext { CurrentTenantType = TenantType.Accountant };
        var businesses = new TenantScopedBusinessRepository(tenant);
        var reconciliation = new FakeReconciliationRepository();
        var cashflow = new FakeCashflowRepository();
        var clock = new FakeClock(Today);

        return new Sut(
            new GetAccountantDashboardHandler(businesses, reconciliation, cashflow, tenant, clock),
            new GetAccountantAlertsHandler(businesses, reconciliation, cashflow, tenant, clock),
            new GetAccountantReportHandler(businesses, reconciliation, cashflow, tenant, clock),
            businesses,
            tenant);
    }

    private static Business Seed(Sut sut, string name, string afm, Guid tenantId)
    {
        var business = Business.Create(name, afm, null, null);
        business.TenantId = tenantId;
        sut.Businesses.Store[business.Id] = business;
        return business;
    }

    [Fact]
    public async Task Dashboard_returns_only_the_signed_in_accountants_businesses()
    {
        var sut = BuildSut();
        var mine = Seed(sut, "Mine", MineAfm, sut.Tenant.CurrentTenantId);
        Seed(sut, "Theirs", TheirsAfm, Guid.NewGuid());

        var result = await sut.Dashboard.Handle(new GetAccountantDashboardQuery());

        var dashboard = result.Should().BeOfType<GetAccountantDashboardResult.Success>()
            .Which.Dashboard;

        dashboard.Summary.TotalBusinesses.Should().Be(1);
        dashboard.Businesses.Should().ContainSingle()
            .Which.BusinessId.Should().Be(mine.Id);
        dashboard.Businesses.Should().NotContain(b => b.Name == "Theirs");
    }

    [Fact]
    public async Task Report_consolidates_only_the_signed_in_accountants_businesses()
    {
        var sut = BuildSut();
        var mine = Seed(sut, "Mine", MineAfm, sut.Tenant.CurrentTenantId);
        Seed(sut, "Theirs", TheirsAfm, Guid.NewGuid());

        var result = await sut.Report.Handle(new GetAccountantReportQuery());

        var report = result.Should().BeOfType<GetAccountantReportResult.Success>().Which.Report;

        report.ReconciliationSummary.Should().ContainSingle()
            .Which.BusinessId.Should().Be(mine.Id);
        report.CashflowOverview.Should().ContainSingle()
            .Which.BusinessId.Should().Be(mine.Id);
    }

    [Fact]
    public async Task Alerts_never_name_another_tenants_business()
    {
        var sut = BuildSut();
        var theirs = Seed(sut, "Theirs", TheirsAfm, Guid.NewGuid());
        theirs.RecordAadeSyncFailure("InvalidCredentials");

        var result = await sut.Alerts.Handle(new GetAccountantAlertsQuery());

        // The only business in the store belongs to somebody else, and it has a
        // failure that would otherwise raise an alert.
        result.Should().BeOfType<GetAccountantAlertsResult.Success>()
            .Which.Alerts.Should().BeEmpty();
    }

    /// <summary>
    /// A business repository that applies the tenant filter the real one gets
    /// from EF, while the <c>...AcrossAllTenantsAsync</c> members deliberately
    /// still see everything — exactly the asymmetry production has.
    /// </summary>
    private sealed class TenantScopedBusinessRepository : IBusinessRepository
    {
        private readonly ITenantContext _tenant;

        public TenantScopedBusinessRepository(ITenantContext tenant)
        {
            ArgumentNullException.ThrowIfNull(tenant);
            _tenant = tenant;
        }

        public Dictionary<Guid, Business> Store { get; } = [];

        private IEnumerable<Business> Scoped
            => Store.Values.Where(b => b.TenantId == _tenant.CurrentTenantId);

        public Task<Business?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Scoped.FirstOrDefault(b => b.Id == id));

        public Task<Business?> GetByIdActiveOnlyAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Scoped.FirstOrDefault(b => b.Id == id && b.IsActive));

        public Task<Business?> FindByAfmAsync(string afm, bool activeOnly, CancellationToken cancellationToken = default)
            => Task.FromResult(Scoped.FirstOrDefault(b => b.Afm == afm && (!activeOnly || b.IsActive)));

        public Task<IReadOnlyList<Business>> ListActiveAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Business>>(
                [.. Scoped.Where(b => b.IsActive).OrderBy(b => b.Name, StringComparer.Ordinal)]);

        public Task<IReadOnlyList<Guid>> ListIdsWithAadeCredentialsAcrossAllTenantsAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Guid>>([.. Store.Values.Where(b => b.IsActive).Select(b => b.Id)]);

        public Task<Business?> GetByIdActiveOnlyAcrossAllTenantsAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Store.Values.FirstOrDefault(b => b.Id == id && b.IsActive));

        public Task<IReadOnlyList<Guid>> ListIdsWithBankingCredentialsAcrossAllTenantsAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Guid>>([.. Store.Values.Where(b => b.IsActive).Select(b => b.Id)]);

        public Task<bool> AfmExistsAsync(
            string afm, bool activeOnly, Guid? excludeId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(Scoped.Any(b => b.Afm == afm
                && (!activeOnly || b.IsActive)
                && (excludeId is null || b.Id != excludeId)));

        public Task<Business> AddAsync(Business business, CancellationToken cancellationToken = default)
        {
            Store[business.Id] = business;
            return Task.FromResult(business);
        }

        public Task<Business> UpdateAsync(Business business, CancellationToken cancellationToken = default)
        {
            Store[business.Id] = business;
            return Task.FromResult(business);
        }

        public Task<IReadOnlyList<Business>> ListWithExpiredAadeFailureAsync(
            DateTime threshold, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Business>>([.. Store.Values.Where(b => b.HasAadeFailure)]);

        public Task<IReadOnlyList<Business>> ListWithExpiredBankingFailureAsync(
            DateTime threshold, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Business>>([.. Store.Values.Where(b => b.BankingSyncErrorCount > 0)]);

        public Task<string?> GetTenantPrimaryEmailAsync(Guid tenantId, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);
    }
}
