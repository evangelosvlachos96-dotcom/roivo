using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Roivo.Application.Abstractions;
using Roivo.Banking;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Enums;
using Roivo.Infrastructure.Persistence;

namespace Roivo.Banking.Tests;

/// <summary>
/// Covers the store's own behaviour — protect on write, unprotect on read, and
/// degrade to "not connected" when the cipher-text can't be read — against an
/// in-memory context. The SQL itself is exercised by the migration, not here.
/// </summary>
public sealed class EncryptedBankingCredentialStoreTests : IDisposable
{
    private const string ValidAfm = "094014201";
    private const string SessionId = "497f6eca-6276-4993-bfeb-53cbbbba6f08";

    private readonly ServiceProvider _services;
    private readonly IDbContextFactory<ApplicationDbContext> _factory;
    private readonly IDataProtectionProvider _protection;

    public EncryptedBankingCredentialStoreTests()
    {
        var databaseName = Guid.NewGuid().ToString();

        _services = new ServiceCollection()
            .AddDataProtection().Services
            .AddSingleton<ITenantContext>(new StubTenantContext())
            .AddDbContextFactory<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName))
            .BuildServiceProvider();

        _factory = _services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        _protection = _services.GetRequiredService<IDataProtectionProvider>();
    }

    [Fact]
    public async Task StoreThenRetrieve_RoundTripsTheSessionId()
    {
        var business = await SeedBusinessAsync();
        var store = CreateStore();

        await store.StoreAsync(business.Id, SessionId, TestCancellation);

        (await store.RetrieveAsync(business.Id, TestCancellation)).Should().Be(SessionId);
    }

    [Fact]
    public async Task StoreAsync_WritesCipherTextNotPlaintext()
    {
        var business = await SeedBusinessAsync();
        var store = CreateStore();

        await store.StoreAsync(business.Id, SessionId, TestCancellation);

        await using var db = await _factory.CreateDbContextAsync(TestCancellation);
        var stored = await db.Businesses
            .IgnoreQueryFilters()
            .Where(b => b.Id == business.Id)
            .Select(b => b.BankingAccessTokenEncrypted)
            .SingleAsync(TestCancellation);

        stored.Should().NotBeNullOrEmpty();
        stored.Should().NotContain(SessionId);
    }

    [Fact]
    public async Task RetrieveAsync_ReturnsNullWhenNothingWasEverStored()
    {
        var business = await SeedBusinessAsync();

        (await CreateStore().RetrieveAsync(business.Id, TestCancellation)).Should().BeNull();
    }

    [Fact]
    public async Task RetrieveAsync_ReturnsNullForAnUnknownBusiness()
    {
        (await CreateStore().RetrieveAsync(Guid.NewGuid(), TestCancellation)).Should().BeNull();
    }

    [Fact]
    public async Task RetrieveAsync_ReturnsNullWhenTheCipherTextCannotBeRead()
    {
        var business = await SeedBusinessAsync();
        await CreateStore().StoreAsync(business.Id, SessionId, TestCancellation);

        // A different purpose stands in for key-ring drift: same provider, but the
        // cipher-text was not produced for this protector, so unprotect throws.
        var foreignStore = new EncryptedBankingCredentialStore(
            new PurposeShiftingProvider(_protection),
            _factory,
            NullLogger<EncryptedBankingCredentialStore>.Instance);

        (await foreignStore.RetrieveAsync(business.Id, TestCancellation)).Should().BeNull();
    }

    [Fact]
    public async Task ClearAsync_RemovesTheStoredSession()
    {
        var business = await SeedBusinessAsync();
        var store = CreateStore();
        await store.StoreAsync(business.Id, SessionId, TestCancellation);

        await store.ClearAsync(business.Id, TestCancellation);

        (await store.RetrieveAsync(business.Id, TestCancellation)).Should().BeNull();
        (await store.HasCredentialsAsync(business.Id, TestCancellation)).Should().BeFalse();
    }

    [Fact]
    public async Task ClearAsync_IsANoOpForAnUnknownBusiness()
    {
        var act = () => CreateStore().ClearAsync(Guid.NewGuid(), TestCancellation);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task HasCredentialsAsync_TracksWhetherASessionIsStored()
    {
        var business = await SeedBusinessAsync();
        var store = CreateStore();

        (await store.HasCredentialsAsync(business.Id, TestCancellation)).Should().BeFalse();

        await store.StoreAsync(business.Id, SessionId, TestCancellation);

        (await store.HasCredentialsAsync(business.Id, TestCancellation)).Should().BeTrue();
    }

    [Fact]
    public async Task StoreAsync_ThrowsForAnUnknownBusiness()
    {
        var act = () => CreateStore().StoreAsync(Guid.NewGuid(), SessionId, TestCancellation);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task StoreAsync_RejectsABlankSessionId()
    {
        var business = await SeedBusinessAsync();

        var act = () => CreateStore().StoreAsync(business.Id, "   ", TestCancellation);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    private static CancellationToken TestCancellation => CancellationToken.None;

    private EncryptedBankingCredentialStore CreateStore()
        => new(_protection, _factory, NullLogger<EncryptedBankingCredentialStore>.Instance);

    private async Task<Business> SeedBusinessAsync()
    {
        var business = Business.Create("Acme", ValidAfm, null, null);
        business.TenantId = StubTenantContext.TenantId;

        await using var db = await _factory.CreateDbContextAsync(TestCancellation);
        db.Businesses.Add(business);
        await db.SaveChangesAsync(TestCancellation);
        return business;
    }

    private sealed class StubTenantContext : ITenantContext
    {
        public static readonly Guid TenantId = Guid.NewGuid();

        public Guid CurrentTenantId => TenantId;
        public TenantType CurrentTenantType => TenantType.Business;
    }

    /// <summary>Hands out protectors under a different purpose, so cipher-text
    /// written by the real store cannot be read back.</summary>
    private sealed class PurposeShiftingProvider : IDataProtectionProvider
    {
        private readonly IDataProtectionProvider _inner;

        public PurposeShiftingProvider(IDataProtectionProvider inner) => _inner = inner;

        public IDataProtector CreateProtector(string purpose)
            => _inner.CreateProtector(purpose + ".Other");
    }

    public void Dispose() => _services.Dispose();
}
