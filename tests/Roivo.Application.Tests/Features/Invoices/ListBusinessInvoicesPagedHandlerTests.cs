using FluentAssertions;
using Roivo.Application.Features.Invoices.Queries.ListBusinessInvoicesPaged;
using Roivo.Application.Tests.Fakes;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Tests.Features.Invoices;

public class ListBusinessInvoicesPagedHandlerTests
{
    private const string ValidAfm = "094014201";

    private sealed record Sut(
        ListBusinessInvoicesPagedHandler Handler,
        FakeBusinessRepository BusinessRepo,
        FakeInvoiceQueryRepository QueryRepo,
        Business Business);

    private static Sut BuildSut()
    {
        var businesses = new FakeBusinessRepository();
        var queries = new FakeInvoiceQueryRepository();
        var business = Business.Create("Acme", ValidAfm, null, null);
        businesses.Store[business.Id] = business;
        return new Sut(new ListBusinessInvoicesPagedHandler(businesses, queries), businesses, queries, business);
    }

    private static InvoiceRow IncRow(DateTime date, string afm = "111111111", decimal gross = 100m, bool cancelled = false, string mark = "M1")
        => new(date, "Incoming", mark, afm, "Name", "1.1", gross * 0.8m, gross * 0.2m, gross, cancelled, null);

    private static InvoiceRow OutRow(DateTime date, string afm = "222222222", decimal gross = 100m, int aggCount = 1)
        => new(date, "Outgoing", null, afm, null, "1.1", gross * 0.8m, gross * 0.2m, gross, false, aggCount);

    private static ListBusinessInvoicesPagedQuery Query(
        Guid businessId,
        int page = 1,
        int pageSize = 25,
        InvoiceDirectionFilter direction = InvoiceDirectionFilter.All,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? search = null,
        CancelledFilter cancelled = CancelledFilter.All)
        => new(businessId, page, pageSize, direction, fromDate, toDate, search, cancelled);

    [Fact]
    public async Task BusinessNotFound_when_business_missing()
    {
        var sut = BuildSut();

        var result = await sut.Handler.Handle(Query(Guid.NewGuid()));

        result.Should().BeOfType<ListBusinessInvoicesPagedResult.BusinessNotFound>();
    }

    [Fact]
    public async Task Returns_rows_from_both_tables_unioned()
    {
        var sut = BuildSut();
        sut.QueryRepo.Rows.AddRange(new[]
        {
            IncRow(new DateTime(2026, 1, 10)),
            IncRow(new DateTime(2026, 1, 11)),
            OutRow(new DateTime(2026, 1, 12)),
        });

        var result = await sut.Handler.Handle(Query(sut.Business.Id));

        var success = result.Should().BeOfType<ListBusinessInvoicesPagedResult.Success>().Subject;
        success.TotalCount.Should().Be(3);
        success.Rows.Should().Contain(r => r.Direction == "Incoming");
        success.Rows.Should().Contain(r => r.Direction == "Outgoing");
    }

    [Fact]
    public async Task Direction_filter_narrows_to_incoming()
    {
        var sut = BuildSut();
        sut.QueryRepo.Rows.AddRange(new[]
        {
            IncRow(new DateTime(2026, 1, 10)),
            IncRow(new DateTime(2026, 1, 11)),
            OutRow(new DateTime(2026, 1, 12)),
        });

        var result = await sut.Handler.Handle(Query(sut.Business.Id, direction: InvoiceDirectionFilter.Incoming));

        var success = result.Should().BeOfType<ListBusinessInvoicesPagedResult.Success>().Subject;
        success.TotalCount.Should().Be(2);
        success.Rows.Should().OnlyContain(r => r.Direction == "Incoming");
    }

    [Fact]
    public async Task Date_filter_narrows_range()
    {
        var sut = BuildSut();
        sut.QueryRepo.Rows.AddRange(new[]
        {
            IncRow(new DateTime(2026, 1, 5)),
            IncRow(new DateTime(2026, 1, 15)),
            IncRow(new DateTime(2026, 1, 25)),
        });

        var result = await sut.Handler.Handle(Query(
            sut.Business.Id,
            fromDate: new DateTime(2026, 1, 10),
            toDate: new DateTime(2026, 1, 20)));

        var success = result.Should().BeOfType<ListBusinessInvoicesPagedResult.Success>().Subject;
        success.TotalCount.Should().Be(1);
        success.Rows.Single().IssueDate.Should().Be(new DateTime(2026, 1, 15));
    }

    [Fact]
    public async Task CounterpartyAfmSearch_finds_matching_rows()
    {
        var sut = BuildSut();
        sut.QueryRepo.Rows.AddRange(new[]
        {
            IncRow(new DateTime(2026, 1, 10), afm: "123456789"),
            IncRow(new DateTime(2026, 1, 11), afm: "987654321"),
        });

        var result = await sut.Handler.Handle(Query(sut.Business.Id, search: "1234"));

        var success = result.Should().BeOfType<ListBusinessInvoicesPagedResult.Success>().Subject;
        success.TotalCount.Should().Be(1);
        success.Rows.Single().CounterpartyAfm.Should().Be("123456789");
    }

    [Fact]
    public async Task HideCancelled_excludes_cancelled_rows()
    {
        var sut = BuildSut();
        sut.QueryRepo.Rows.AddRange(new[]
        {
            IncRow(new DateTime(2026, 1, 10), cancelled: false),
            IncRow(new DateTime(2026, 1, 11), cancelled: true),
        });

        var result = await sut.Handler.Handle(Query(sut.Business.Id, cancelled: CancelledFilter.HideCancelled));

        var success = result.Should().BeOfType<ListBusinessInvoicesPagedResult.Success>().Subject;
        success.TotalCount.Should().Be(1);
        success.Rows.Should().OnlyContain(r => !r.IsCancelled);
    }

    [Fact]
    public async Task Pagination_returns_correct_count_and_page()
    {
        var sut = BuildSut();
        for (var day = 1; day <= 30; day++)
            sut.QueryRepo.Rows.Add(IncRow(new DateTime(2026, 1, day)));

        // Sorted IssueDate descending by default: page 2 of 10 starts at day 20.
        var result = await sut.Handler.Handle(Query(sut.Business.Id, page: 2, pageSize: 10));

        var success = result.Should().BeOfType<ListBusinessInvoicesPagedResult.Success>().Subject;
        success.TotalCount.Should().Be(30);
        success.Rows.Should().HaveCount(10);
        success.Rows.First().IssueDate.Should().Be(new DateTime(2026, 1, 20));
    }

    [Fact]
    public async Task PageSize_is_clamped_to_max()
    {
        var sut = BuildSut();
        sut.QueryRepo.Rows.Add(IncRow(new DateTime(2026, 1, 1)));

        var result = await sut.Handler.Handle(Query(sut.Business.Id, pageSize: 5000));

        // Clamp to 100 means a single row still returns; the point is no overflow/throw.
        result.Should().BeOfType<ListBusinessInvoicesPagedResult.Success>()
            .Which.Rows.Should().ContainSingle();
    }
}
