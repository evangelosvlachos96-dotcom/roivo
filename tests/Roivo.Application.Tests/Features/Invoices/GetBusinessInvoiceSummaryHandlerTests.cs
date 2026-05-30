using FluentAssertions;
using Roivo.Application.Features.Invoices.Queries.GetBusinessInvoiceSummary;
using Roivo.Application.Features.Invoices.Queries.ListBusinessInvoicesPaged;
using Roivo.Application.Tests.Fakes;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Tests.Features.Invoices;

public class GetBusinessInvoiceSummaryHandlerTests
{
    private const string ValidAfm = "094014201";

    private sealed record Sut(
        GetBusinessInvoiceSummaryHandler Handler,
        FakeBusinessRepository BusinessRepo,
        FakeInvoiceQueryRepository QueryRepo);

    private static Sut BuildSut()
    {
        var businesses = new FakeBusinessRepository();
        var queries = new FakeInvoiceQueryRepository();
        return new Sut(new GetBusinessInvoiceSummaryHandler(businesses, queries), businesses, queries);
    }

    private static InvoiceRow Inc(decimal gross, string afm = "111111111")
        => new(new DateTime(2026, 1, 10), "Incoming", "M", afm, "Name", "1.1", gross * 0.8m, gross * 0.2m, gross, false, null);

    private static InvoiceRow Out(decimal gross, int aggCount, string afm = "222222222")
        => new(new DateTime(2026, 1, 11), "Outgoing", null, afm, null, "1.1", gross * 0.8m, gross * 0.2m, gross, false, aggCount);

    [Fact]
    public async Task Success_returns_correct_totals_and_sync_timestamp()
    {
        var sut = BuildSut();
        var business = Business.Create("Acme", ValidAfm, null, null);
        var syncedAt = new DateTime(2026, 2, 1, 9, 30, 0, DateTimeKind.Utc);
        business.RecordAadeSync(syncedAt);
        sut.BusinessRepo.Store[business.Id] = business;

        sut.QueryRepo.Rows.AddRange(new[]
        {
            Inc(100m), Inc(200m),
            Out(50m, aggCount: 3), Out(70m, aggCount: 2),
        });

        var result = await sut.Handler.Handle(new GetBusinessInvoiceSummaryQuery(business.Id));

        var success = result.Should().BeOfType<GetBusinessInvoiceSummaryResult.Success>().Subject;
        success.Business.Name.Should().Be("Acme");
        success.Business.Afm.Should().Be(ValidAfm);

        var data = success.Data;
        data.IncomingGrossTotal.Should().Be(300m);
        data.IncomingCount.Should().Be(2);
        data.OutgoingGrossTotal.Should().Be(120m);
        data.OutgoingAggregateCount.Should().Be(2);
        data.OutgoingInvoiceCount.Should().Be(5);
        data.LastSyncedAt.Should().Be(syncedAt);
        data.Recent.Should().HaveCount(4);
    }

    [Fact]
    public async Task BusinessNotFound_when_business_missing()
    {
        var sut = BuildSut();

        var result = await sut.Handler.Handle(new GetBusinessInvoiceSummaryQuery(Guid.NewGuid()));

        result.Should().BeOfType<GetBusinessInvoiceSummaryResult.BusinessNotFound>();
    }
}
