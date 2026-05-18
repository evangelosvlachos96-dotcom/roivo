using FluentAssertions;
using Roivo.Application.Features.Businesses.Queries.GetBusinessById;
using Roivo.Application.Features.Businesses.Queries.ListActiveBusinesses;
using Roivo.Application.Tests.Fakes;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Tests.Features.Businesses;

public class BusinessQueryHandlerTests
{
    private const string ValidAfm = "094014201";
    private const string AnotherValidAfm = "123456783";
    private const string ThirdValidAfm = "999074658";

    [Fact]
    public async Task GetBusinessById_active_only_returns_match()
    {
        var repo = new FakeBusinessRepository();
        var b = Business.Create("Acme", ValidAfm, null, null);
        repo.Store[b.Id] = b;
        var handler = new GetBusinessByIdHandler(repo);

        var result = await handler.Handle(new GetBusinessByIdQuery(b.Id, ActiveOnly: true));

        result.Should().NotBeNull();
        result!.Id.Should().Be(b.Id);
    }

    [Fact]
    public async Task GetBusinessById_active_only_hides_inactive()
    {
        var repo = new FakeBusinessRepository();
        var b = Business.Create("Acme", ValidAfm, null, null);
        b.Deactivate();
        repo.Store[b.Id] = b;
        var handler = new GetBusinessByIdHandler(repo);

        var result = await handler.Handle(new GetBusinessByIdQuery(b.Id, ActiveOnly: true));

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBusinessById_without_active_filter_returns_inactive()
    {
        var repo = new FakeBusinessRepository();
        var b = Business.Create("Acme", ValidAfm, null, null);
        b.Deactivate();
        repo.Store[b.Id] = b;
        var handler = new GetBusinessByIdHandler(repo);

        var result = await handler.Handle(new GetBusinessByIdQuery(b.Id, ActiveOnly: false));

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ListActiveBusinesses_returns_only_active_ordered_by_name()
    {
        var repo = new FakeBusinessRepository();
        var bravo = Business.Create("Bravo", ValidAfm, null, null);
        var alpha = Business.Create("Alpha", AnotherValidAfm, null, null);
        var charlie = Business.Create("Charlie", ThirdValidAfm, null, null);
        charlie.Deactivate();
        repo.Store[bravo.Id] = bravo;
        repo.Store[alpha.Id] = alpha;
        repo.Store[charlie.Id] = charlie;
        var handler = new ListActiveBusinessesHandler(repo);

        var result = await handler.Handle(new ListActiveBusinessesQuery());

        result.Should().HaveCount(2);
        result.Select(b => b.Name).Should().ContainInOrder("Alpha", "Bravo");
    }
}
