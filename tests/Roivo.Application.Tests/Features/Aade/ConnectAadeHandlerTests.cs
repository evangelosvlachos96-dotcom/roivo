using FluentAssertions;
using Roivo.Application.Abstractions.Aade.Results;
using Roivo.Application.Features.Aade.Commands.ConnectAade;
using Roivo.Application.Tests.Fakes;
using Roivo.Core.Domain.Auditing;
using Roivo.Core.Domain.Entities;

namespace Roivo.Application.Tests.Features.Aade;

public class ConnectAadeHandlerTests
{
    private const string ValidAfm = "094014201";

    private sealed record Sut(
        ConnectAadeHandler Handler,
        FakeBusinessRepository BusinessRepo,
        FakeAadeClient Client,
        FakeAadeCredentialStore Store,
        FakeAuditWriter Audit);

    private static Sut BuildSut()
    {
        var businesses = new FakeBusinessRepository();
        var client = new FakeAadeClient();
        var store = new FakeAadeCredentialStore();
        var audit = new FakeAuditWriter();
        var tenant = new FakeTenantContext();
        return new Sut(
            new ConnectAadeHandler(businesses, client, store, audit, tenant),
            businesses, client, store, audit);
    }

    private static Business Seed(FakeBusinessRepository repo, string afm = ValidAfm)
    {
        var b = Business.Create("Acme", afm, null, null);
        repo.Store[b.Id] = b;
        return b;
    }

    [Fact]
    public async Task Success_path_stores_credentials_and_audits()
    {
        var sut = BuildSut();
        var b = Seed(sut.BusinessRepo);
        sut.Client.NextValidation = new AadeValidationResult.Success();

        var result = await sut.Handler.Handle(new ConnectAadeCommand(b.Id, "user-123", "subkey-abc"));

        result.Should().BeOfType<ConnectAadeResult.Success>().Which.BusinessId.Should().Be(b.Id);
        sut.Store.Store.Should().ContainKey(b.Id);
        sut.Audit.Calls.Should().ContainSingle().Which.Action.Should().Be(AuditAction.AadeConnected);
        // Verification call must pass the business's AFM through.
        sut.Client.ValidationCalls.Should().ContainSingle()
            .Which.BusinessAfm.Should().Be(ValidAfm);
    }

    [Fact]
    public async Task BusinessNotFound_when_business_missing()
    {
        var sut = BuildSut();

        var result = await sut.Handler.Handle(new ConnectAadeCommand(Guid.NewGuid(), "u", "k"));

        result.Should().BeOfType<ConnectAadeResult.BusinessNotFound>();
        sut.Store.Store.Should().BeEmpty();
        sut.Audit.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task InvalidCredentials_when_AADE_rejects_key()
    {
        var sut = BuildSut();
        var b = Seed(sut.BusinessRepo);
        sut.Client.NextValidation = new AadeValidationResult.InvalidCredentials();

        var result = await sut.Handler.Handle(new ConnectAadeCommand(b.Id, "u", "bad-key"));

        result.Should().BeOfType<ConnectAadeResult.InvalidCredentials>();
        sut.Store.Store.Should().BeEmpty();
    }

    [Fact]
    public async Task AfmMismatch_carries_business_and_credentials_AFMs_when_extracted()
    {
        var sut = BuildSut();
        var b = Seed(sut.BusinessRepo);
        sut.Client.NextValidation = new AadeValidationResult.AfmMismatch("123456783");

        var result = await sut.Handler.Handle(new ConnectAadeCommand(b.Id, "u", "k"));

        var mismatch = result.Should().BeOfType<ConnectAadeResult.AfmMismatch>().Subject;
        mismatch.BusinessAfm.Should().Be(ValidAfm);
        mismatch.CredentialsAfm.Should().Be("123456783");
        sut.Store.Store.Should().BeEmpty();
        sut.Audit.Calls.Should().ContainSingle()
            .Which.Action.Should().Be(AuditAction.AadeConnectionAfmMismatch);
    }

    [Fact]
    public async Task AfmMismatch_returns_null_CredentialsAfm_when_extraction_fails()
    {
        var sut = BuildSut();
        var b = Seed(sut.BusinessRepo);
        sut.Client.NextValidation = new AadeValidationResult.AfmMismatch(null);

        var result = await sut.Handler.Handle(new ConnectAadeCommand(b.Id, "u", "k"));

        var mismatch = result.Should().BeOfType<ConnectAadeResult.AfmMismatch>().Subject;
        mismatch.BusinessAfm.Should().Be(ValidAfm);
        mismatch.CredentialsAfm.Should().BeNull();
        sut.Store.Store.Should().BeEmpty();
    }

    [Fact]
    public async Task RateLimited_when_AADE_returns_429()
    {
        var sut = BuildSut();
        var b = Seed(sut.BusinessRepo);
        sut.Client.NextValidation = new AadeValidationResult.RateLimited();

        var result = await sut.Handler.Handle(new ConnectAadeCommand(b.Id, "u", "k"));

        result.Should().BeOfType<ConnectAadeResult.RateLimited>();
        sut.Store.Store.Should().BeEmpty();
    }

    [Fact]
    public async Task AadeUnavailable_on_network_error()
    {
        var sut = BuildSut();
        var b = Seed(sut.BusinessRepo);
        sut.Client.NextValidation = new AadeValidationResult.NetworkError("DNS failure");

        var result = await sut.Handler.Handle(new ConnectAadeCommand(b.Id, "u", "k"));

        result.Should().BeOfType<ConnectAadeResult.AadeUnavailable>()
            .Which.Message.Should().Contain("DNS failure");
        sut.Store.Store.Should().BeEmpty();
    }

    [Fact]
    public async Task AadeUnavailable_on_server_error()
    {
        var sut = BuildSut();
        var b = Seed(sut.BusinessRepo);
        sut.Client.NextValidation = new AadeValidationResult.AadeServerError(503, "Service down");

        var result = await sut.Handler.Handle(new ConnectAadeCommand(b.Id, "u", "k"));

        result.Should().BeOfType<ConnectAadeResult.AadeUnavailable>();
    }

    [Fact]
    public async Task Audit_does_not_contain_credentials()
    {
        var sut = BuildSut();
        var b = Seed(sut.BusinessRepo);
        sut.Client.NextValidation = new AadeValidationResult.Success();

        await sut.Handler.Handle(new ConnectAadeCommand(b.Id, "secret-user-id", "secret-subscription-key"));

        var details = sut.Audit.Calls.Single().Details;
        var serialized = System.Text.Json.JsonSerializer.Serialize(details);
        serialized.Should().NotContain("secret-user-id");
        serialized.Should().NotContain("secret-subscription-key");
    }
}
