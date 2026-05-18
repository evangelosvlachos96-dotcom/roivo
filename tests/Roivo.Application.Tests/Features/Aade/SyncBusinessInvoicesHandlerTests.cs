using FluentAssertions;
using Roivo.Application.Abstractions.Aade;
using Roivo.Application.Abstractions.Aade.Results;
using Roivo.Application.Features.Aade.Commands.SyncBusinessInvoices;
using Roivo.Application.Tests.Fakes;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Tests.Features.Aade;

public class SyncBusinessInvoicesHandlerTests
{
    private const string ValidAfm = "094014201";

    private sealed record Sut(
        SyncBusinessInvoicesHandler Handler,
        FakeBusinessRepository BusinessRepo,
        FakeInvoiceRepository InvoiceRepo,
        FakeAadeClient Client,
        FakeAadeCredentialStore Store,
        FakeAuditWriter Audit);

    private static Sut BuildSut()
    {
        var businesses = new FakeBusinessRepository();
        var invoices = new FakeInvoiceRepository();
        var client = new FakeAadeClient();
        var store = new FakeAadeCredentialStore();
        var audit = new FakeAuditWriter();
        var tenant = new FakeTenantContext();
        return new Sut(
            new SyncBusinessInvoicesHandler(businesses, invoices, client, store, audit, tenant),
            businesses, invoices, client, store, audit);
    }

    private static Business SeedConnected(Sut sut, string afm = ValidAfm)
    {
        var b = Business.Create("Acme", afm, null, null);
        sut.BusinessRepo.Store[b.Id] = b;
        sut.Store.Store[b.Id] = new AadeCredentials("user", "key");
        return b;
    }

    private static AadeInvoiceDto Inv(string mark, decimal amount = 100m)
        => new(
            Mark: mark,
            CounterpartyAfm: "999999999",
            CounterpartyName: "Counterparty",
            IssueDate: new DateTime(2026, 1, 15),
            DocumentTypeCode: "1.1",
            GrossAmount: amount,
            Currency: "EUR",
            RawXml: $"<invoice><mark>{mark}</mark></invoice>");

    [Fact]
    public async Task BusinessNotFound_when_business_missing()
    {
        var sut = BuildSut();
        var result = await sut.Handler.Handle(new SyncBusinessInvoicesCommand(Guid.NewGuid()));
        result.Should().BeOfType<SyncBusinessInvoicesResult.BusinessNotFound>();
    }

    [Fact]
    public async Task NotConnected_when_credentials_missing()
    {
        var sut = BuildSut();
        var b = Business.Create("Acme", ValidAfm, null, null);
        sut.BusinessRepo.Store[b.Id] = b;
        // No credentials stored.

        var result = await sut.Handler.Handle(new SyncBusinessInvoicesCommand(b.Id));

        result.Should().BeOfType<SyncBusinessInvoicesResult.NotConnected>();
    }

    [Fact]
    public async Task Success_with_new_invoices_upserts_and_sets_sync_timestamp()
    {
        var sut = BuildSut();
        var b = SeedConnected(sut);
        sut.Client.NextFetch = new AadeFetchResult.Success(
            Incoming: new[] { Inv("INC-1"), Inv("INC-2") },
            Outgoing: new[] { Inv("OUT-1") });

        var result = await sut.Handler.Handle(new SyncBusinessInvoicesCommand(b.Id));

        var success = result.Should().BeOfType<SyncBusinessInvoicesResult.Success>().Subject;
        success.NewCount.Should().Be(3);
        success.UpdatedCount.Should().Be(0);
        sut.InvoiceRepo.ByMark.Should().HaveCount(3);
        sut.BusinessRepo.Store[b.Id].LastAadeSyncAt.Should().NotBeNull();
        sut.Audit.Calls.Should().ContainSingle().Which.Action.Should().Be("AadeSyncCompleted");
    }

    [Fact]
    public async Task Success_with_no_invoices_still_sets_sync_timestamp()
    {
        var sut = BuildSut();
        var b = SeedConnected(sut);
        sut.Client.NextFetch = new AadeFetchResult.Success(
            Incoming: Array.Empty<AadeInvoiceDto>(),
            Outgoing: Array.Empty<AadeInvoiceDto>());

        var result = await sut.Handler.Handle(new SyncBusinessInvoicesCommand(b.Id));

        result.Should().BeOfType<SyncBusinessInvoicesResult.Success>()
            .Which.NewCount.Should().Be(0);
        sut.BusinessRepo.Store[b.Id].LastAadeSyncAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Existing_invoice_with_same_mark_is_updated_not_duplicated()
    {
        var sut = BuildSut();
        var b = SeedConnected(sut);
        sut.InvoiceRepo.ByMark["INC-1"] = new Invoice
        {
            BusinessId = b.Id,
            AadeMark = "INC-1",
            InvoiceType = "1.1",
            CounterpartyAfm = "999999999",
            GrossAmount = 50m,
        };
        sut.Client.NextFetch = new AadeFetchResult.Success(
            Incoming: new[] { Inv("INC-1", amount: 200m) },
            Outgoing: Array.Empty<AadeInvoiceDto>());

        var result = await sut.Handler.Handle(new SyncBusinessInvoicesCommand(b.Id));

        var success = result.Should().BeOfType<SyncBusinessInvoicesResult.Success>().Subject;
        success.NewCount.Should().Be(0);
        success.UpdatedCount.Should().Be(1);
        sut.InvoiceRepo.ByMark.Should().HaveCount(1);
        sut.InvoiceRepo.ByMark["INC-1"].GrossAmount.Should().Be(200m);
    }

    [Fact]
    public async Task NetworkError_surfaces_directly()
    {
        var sut = BuildSut();
        var b = SeedConnected(sut);
        sut.Client.NextFetch = new AadeFetchResult.NetworkError("conn refused");

        var result = await sut.Handler.Handle(new SyncBusinessInvoicesCommand(b.Id));

        result.Should().BeOfType<SyncBusinessInvoicesResult.NetworkError>()
            .Which.Message.Should().Be("conn refused");
        sut.BusinessRepo.Store[b.Id].LastAadeSyncAt.Should().BeNull();
    }

    [Fact]
    public async Task InvalidCredentials_returns_dedicated_variant()
    {
        var sut = BuildSut();
        var b = SeedConnected(sut);
        sut.Client.NextFetch = new AadeFetchResult.InvalidCredentials();

        var result = await sut.Handler.Handle(new SyncBusinessInvoicesCommand(b.Id));

        result.Should().BeOfType<SyncBusinessInvoicesResult.InvalidCredentials>();
    }
}
