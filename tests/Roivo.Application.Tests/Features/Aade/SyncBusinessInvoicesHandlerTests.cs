using FluentAssertions;
using Roivo.Application.Abstractions.Aade;
using Roivo.Application.Abstractions.Aade.Results;
using Roivo.Application.Features.Aade.Commands.SyncBusinessInvoices;
using Roivo.Application.Tests.Fakes;
using Roivo.Core.Domain.Auditing;
using Roivo.Core.Domain.Entities;
using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Tests.Features.Aade;

public class SyncBusinessInvoicesHandlerTests
{
    private const string ValidAfm = "094014201";

    private sealed record Sut(
        SyncBusinessInvoicesHandler Handler,
        FakeBusinessRepository BusinessRepo,
        FakeInvoiceRepository InvoiceRepo,
        FakeIncomeBookEntryRepository BookRepo,
        FakeAadeClient Client,
        FakeAadeCredentialStore Store,
        FakeAuditWriter Audit);

    private static Sut BuildSut()
    {
        var businesses = new FakeBusinessRepository();
        var invoices = new FakeInvoiceRepository();
        var bookEntries = new FakeIncomeBookEntryRepository();
        var client = new FakeAadeClient();
        var store = new FakeAadeCredentialStore();
        var audit = new FakeAuditWriter();
        var tenant = new FakeTenantContext();
        return new Sut(
            new SyncBusinessInvoicesHandler(businesses, invoices, bookEntries, client, store, audit, tenant),
            businesses, invoices, bookEntries, client, store, audit);
    }

    private static Business SeedConnected(Sut sut, string afm = ValidAfm)
    {
        var b = Business.Create("Acme", afm, null, null);
        sut.BusinessRepo.Store[b.Id] = b;
        sut.Store.Store[b.Id] = new AadeCredentials("user", "key");
        return b;
    }

    private static AadeInvoiceDto Inv(string mark, decimal gross = 124m, string? cancelledByMark = null)
        => new(
            Mark: mark,
            IssuerAfm: "999999999",
            CounterpartyAfm: ValidAfm,
            CounterpartyName: "Counterparty",
            IssueDate: new DateTime(2026, 1, 15),
            DocumentTypeCode: "1.1",
            NetAmount: 100m,
            VatAmount: 24m,
            GrossAmount: gross,
            Currency: "EUR",
            CancelledByMark: cancelledByMark);

    private static AadeBookEntryDto Book(
        string counterAfm = "888888888",
        string docType = "1.1",
        decimal net = 200m, decimal vat = 48m, decimal gross = 248m,
        int count = 2, long minMark = 10, long maxMark = 20)
        => new(
            CounterpartyAfm: counterAfm,
            IssueDate: new DateTime(2026, 1, 16),
            DocumentTypeCode: docType,
            NetValue: net,
            VatAmount: vat,
            GrossValue: gross,
            InvoiceCount: count,
            MinMark: minMark,
            MaxMark: maxMark);

    private static AadeFetchResult.Success Fetched(
        AadeInvoiceDto[] incoming, AadeBookEntryDto[] outgoing, long maxIncoming, long maxOutgoing)
        => new(incoming, outgoing, maxIncoming, maxOutgoing);

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

        var result = await sut.Handler.Handle(new SyncBusinessInvoicesCommand(b.Id));

        result.Should().BeOfType<SyncBusinessInvoicesResult.NotConnected>();
    }

    [Fact]
    public async Task Success_with_incoming_invoices_persists_and_advances_marks()
    {
        var sut = BuildSut();
        var b = SeedConnected(sut);
        sut.Client.NextFetch = Fetched(
            incoming: new[] { Inv("INC-1"), Inv("INC-2") },
            outgoing: Array.Empty<AadeBookEntryDto>(),
            maxIncoming: 500, maxOutgoing: 300);

        var result = await sut.Handler.Handle(new SyncBusinessInvoicesCommand(b.Id));

        var success = result.Should().BeOfType<SyncBusinessInvoicesResult.Success>().Subject;
        success.NewCount.Should().Be(2);
        sut.InvoiceRepo.ByMark.Should().HaveCount(2);

        var stored = sut.BusinessRepo.Store[b.Id];
        stored.LastAadeSyncAt.Should().NotBeNull();
        stored.LastAadeIncomingMark.Should().Be(500);
        stored.LastAadeOutgoingMark.Should().Be(300);
        sut.Audit.Calls.Should().ContainSingle().Which.Action.Should().Be(AuditAction.AadeSyncCompleted);
    }

    [Fact]
    public async Task Success_with_no_data_still_sets_sync_timestamp()
    {
        var sut = BuildSut();
        var b = SeedConnected(sut);
        sut.Client.NextFetch = Fetched(
            Array.Empty<AadeInvoiceDto>(), Array.Empty<AadeBookEntryDto>(),
            maxIncoming: 0, maxOutgoing: 0);

        var result = await sut.Handler.Handle(new SyncBusinessInvoicesCommand(b.Id));

        result.Should().BeOfType<SyncBusinessInvoicesResult.Success>().Which.NewCount.Should().Be(0);
        sut.BusinessRepo.Store[b.Id].LastAadeSyncAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Existing_invoice_with_same_mark_is_updated_not_duplicated()
    {
        var sut = BuildSut();
        var b = SeedConnected(sut);
        sut.InvoiceRepo.ByMark["INC-1"] = Invoice.Create(
            b.Id, "INC-1", InvoiceDirection.Received, "1.1",
            new DateOnly(2026, 1, 15), ValidAfm, "Counterparty",
            50m, 12m, 62m, Currency.EUR, null);
        sut.Client.NextFetch = Fetched(
            incoming: new[] { Inv("INC-1", gross: 248m) },
            outgoing: Array.Empty<AadeBookEntryDto>(),
            maxIncoming: 1, maxOutgoing: 0);

        await sut.Handler.Handle(new SyncBusinessInvoicesCommand(b.Id));

        sut.InvoiceRepo.ByMark.Should().HaveCount(1);
        sut.InvoiceRepo.ByMark["INC-1"].GrossAmount.Should().Be(248m);
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

    [Fact]
    public async Task RespectsExistingMarks_PassesThemToClient()
    {
        var sut = BuildSut();
        var b = SeedConnected(sut);
        b.RecordAadeSyncProgress(newIncomingMark: 12345, newOutgoingMark: 6789);
        sut.Client.NextFetch = Fetched(
            Array.Empty<AadeInvoiceDto>(), Array.Empty<AadeBookEntryDto>(),
            maxIncoming: 12345, maxOutgoing: 6789);

        await sut.Handler.Handle(new SyncBusinessInvoicesCommand(b.Id));

        var call = sut.Client.FetchCalls.Should().ContainSingle().Subject;
        call.SinceIncomingMark.Should().Be(12345);
        call.SinceOutgoingMark.Should().Be(6789);
    }

    [Fact]
    public async Task PersistsBookEntries_WhenOutgoingReturned()
    {
        var sut = BuildSut();
        var b = SeedConnected(sut);
        sut.Client.NextFetch = Fetched(
            Array.Empty<AadeInvoiceDto>(),
            outgoing: new[] { Book(counterAfm: "888888888", net: 200m, vat: 48m, gross: 248m, count: 2, minMark: 10, maxMark: 20) },
            maxIncoming: 0, maxOutgoing: 20);

        await sut.Handler.Handle(new SyncBusinessInvoicesCommand(b.Id));

        sut.BookRepo.Store.Should().ContainSingle();
        var entry = sut.BookRepo.Store.Values.Single();
        entry.CounterpartyAfm.Should().Be("888888888");
        entry.GrossValue.Should().Be(248m);
        entry.InvoiceCount.Should().Be(2);
    }

    [Fact]
    public async Task UpdatesExistingBookEntry_OnSameIdentityTuple()
    {
        var sut = BuildSut();
        var b = SeedConnected(sut);
        var issueDate = new DateTime(2026, 1, 16);
        var existing = IncomeBookEntry.Create(
            b.Id, b.TenantId, "888888888", issueDate, "1.1",
            100m, 24m, 124m, 1, 5, 5);
        sut.BookRepo.Store[(b.Id, "888888888", issueDate, "1.1")] = existing;

        sut.Client.NextFetch = Fetched(
            Array.Empty<AadeInvoiceDto>(),
            outgoing: new[] { Book(counterAfm: "888888888", docType: "1.1", net: 200m, vat: 48m, gross: 248m, count: 2, minMark: 5, maxMark: 20) },
            maxIncoming: 0, maxOutgoing: 20);

        await sut.Handler.Handle(new SyncBusinessInvoicesCommand(b.Id));

        sut.BookRepo.Store.Should().ContainSingle();
        var entry = sut.BookRepo.Store.Values.Single();
        entry.GrossValue.Should().Be(248m);
        entry.InvoiceCount.Should().Be(2);
        entry.MaxMark.Should().Be(20);
    }

    [Fact]
    public async Task AdvancesOutgoingMark_FromBookEntries()
    {
        var sut = BuildSut();
        var b = SeedConnected(sut);
        sut.Client.NextFetch = Fetched(
            Array.Empty<AadeInvoiceDto>(),
            outgoing: new[] { Book(maxMark: 999) },
            maxIncoming: 0, maxOutgoing: 999);

        await sut.Handler.Handle(new SyncBusinessInvoicesCommand(b.Id));

        sut.BusinessRepo.Store[b.Id].LastAadeOutgoingMark.Should().Be(999);
    }

    [Fact]
    public async Task PreservesCancelledByMark_OnIncomingInvoice()
    {
        var sut = BuildSut();
        var b = SeedConnected(sut);
        sut.Client.NextFetch = Fetched(
            incoming: new[] { Inv("INC-9", cancelledByMark: "CANCEL-42") },
            outgoing: Array.Empty<AadeBookEntryDto>(),
            maxIncoming: 9, maxOutgoing: 0);

        await sut.Handler.Handle(new SyncBusinessInvoicesCommand(b.Id));

        sut.InvoiceRepo.ByMark["INC-9"].CancelledByMark.Should().Be("CANCEL-42");
    }

    [Fact]
    public async Task OnInvalidCredentials_MarksBusinessAsFailed()
    {
        var sut = BuildSut();
        var b = SeedConnected(sut);
        sut.Client.NextFetch = new AadeFetchResult.InvalidCredentials();

        await sut.Handler.Handle(new SyncBusinessInvoicesCommand(b.Id));

        var stored = sut.BusinessRepo.Store[b.Id];
        stored.HasAadeFailure.Should().BeTrue();
        stored.AadeLastFailureReason.Should().Be("InvalidCredentials");
        stored.AadeLastFailureAt.Should().NotBeNull();
    }

    [Fact]
    public async Task OnNetworkError_DoesNotMarkAsFailed()
    {
        var sut = BuildSut();
        var b = SeedConnected(sut);
        sut.Client.NextFetch = new AadeFetchResult.NetworkError("conn refused");

        await sut.Handler.Handle(new SyncBusinessInvoicesCommand(b.Id));

        sut.BusinessRepo.Store[b.Id].HasAadeFailure.Should().BeFalse();
    }

    [Fact]
    public async Task OnServerError_DoesNotMarkAsFailed()
    {
        var sut = BuildSut();
        var b = SeedConnected(sut);
        sut.Client.NextFetch = new AadeFetchResult.AadeServerError(503, "down");

        await sut.Handler.Handle(new SyncBusinessInvoicesCommand(b.Id));

        sut.BusinessRepo.Store[b.Id].HasAadeFailure.Should().BeFalse();
    }

    [Fact]
    public async Task OnSuccess_ClearsFailureState()
    {
        var sut = BuildSut();
        var b = SeedConnected(sut);
        b.RecordAadeSyncFailure("InvalidCredentials");
        sut.Client.NextFetch = Fetched(
            Array.Empty<AadeInvoiceDto>(), Array.Empty<AadeBookEntryDto>(),
            maxIncoming: 0, maxOutgoing: 0);

        await sut.Handler.Handle(new SyncBusinessInvoicesCommand(b.Id));

        var stored = sut.BusinessRepo.Store[b.Id];
        stored.HasAadeFailure.Should().BeFalse();
        stored.AadeLastFailureAt.Should().BeNull();
        stored.AadeLastFailureReason.Should().BeNull();
    }
}
