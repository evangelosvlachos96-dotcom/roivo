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

    [Fact]
    public async Task GetBusinessById_active_only_returns_match()
    {
        var repo = new FakeBusinessRepository();
        var id = Guid.NewGuid();
        repo.Store[id] = new Business { Id = id, Name = "Acme", Afm = ValidAfm, IsActive = true };
        var handler = new GetBusinessByIdHandler(repo);

        var result = await handler.Handle(new GetBusinessByIdQuery(id, ActiveOnly: true));

        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetBusinessById_active_only_hides_inactive()
    {
        var repo = new FakeBusinessRepository();
        var id = Guid.NewGuid();
        repo.Store[id] = new Business { Id = id, Name = "Acme", Afm = ValidAfm, IsActive = false };
        var handler = new GetBusinessByIdHandler(repo);

        var result = await handler.Handle(new GetBusinessByIdQuery(id, ActiveOnly: true));

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBusinessById_without_active_filter_returns_inactive()
    {
        var repo = new FakeBusinessRepository();
        var id = Guid.NewGuid();
        repo.Store[id] = new Business { Id = id, Name = "Acme", Afm = ValidAfm, IsActive = false };
        var handler = new GetBusinessByIdHandler(repo);

        var result = await handler.Handle(new GetBusinessByIdQuery(id, ActiveOnly: false));

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ListActiveBusinesses_returns_only_active_ordered_by_name()
    {
        var repo = new FakeBusinessRepository();
        repo.Store[Guid.NewGuid()] = new Business { Name = "Bravo", Afm = ValidAfm, IsActive = true };
        repo.Store[Guid.NewGuid()] = new Business { Name = "Alpha", Afm = AnotherValidAfm, IsActive = true };
        repo.Store[Guid.NewGuid()] = new Business { Name = "Charlie", Afm = "000000010", IsActive = false };
        var handler = new ListActiveBusinessesHandler(repo);

        var result = await handler.Handle(new ListActiveBusinessesQuery());

        result.Should().HaveCount(2);
        result.Select(b => b.Name).Should().ContainInOrder("Alpha", "Bravo");
    }
}
