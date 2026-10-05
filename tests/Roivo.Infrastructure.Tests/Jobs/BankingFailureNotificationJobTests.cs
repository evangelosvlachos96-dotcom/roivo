using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Roivo.Core.Domain.Entities;
using Roivo.Infrastructure.Jobs;
using Roivo.Infrastructure.Tests.Fakes;

namespace Roivo.Infrastructure.Tests.Jobs;

public class BankingFailureNotificationJobTests
{
    private const string ValidAfm = "094014201";

    private sealed record Sut(
        BankingFailureNotificationJob Job,
        FakeBusinessRepository BusinessRepo,
        RecordingEmailSender EmailSender);

    private static Sut BuildSut()
    {
        var businesses = new FakeBusinessRepository();
        var email = new RecordingEmailSender();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["App:PublicBaseUrl"] = "https://test.roivo.gr"
            })
            .Build();
        var job = new BankingFailureNotificationJob(businesses, email, config, NullLogger<BankingFailureNotificationJob>.Instance);
        return new Sut(job, businesses, email);
    }

    /// <summary>
    /// Seeds a connected business with a failure streak of the given age. The
    /// streak-start setter is private, so the test reaches in rather than
    /// introducing a fake clock for a single precondition.
    /// </summary>
    private static Business SeedBrokenBusiness(
        Sut sut,
        TimeSpan failureAge,
        string? ownerEmail = "owner@example.com",
        int failureCount = 1)
    {
        var tenantId = Guid.NewGuid();
        var business = Business.Create("Acme", ValidAfm, null, null);
        business.TenantId = tenantId;
        business.BankingAccessTokenEncrypted = "cipher-text";
        business.RecordBankingConnection("Test Bank", DateTime.UtcNow.AddDays(90));

        for (var i = 0; i < failureCount; i++)
            business.RecordBankingSyncFailure("SessionExpired");

        var prop = typeof(Business).GetProperty(nameof(Business.BankingFirstFailureAt))!;
        prop.SetValue(business, DateTime.UtcNow - failureAge);

        sut.BusinessRepo.Store[business.Id] = business;
        sut.BusinessRepo.TenantPrimaryEmails[tenantId] = ownerEmail;
        return business;
    }

    [Fact]
    public async Task SendsEmailForBusinessesBrokenOver24Hours()
    {
        var sut = BuildSut();
        var business = SeedBrokenBusiness(sut, TimeSpan.FromHours(25));

        await sut.Job.Execute();

        var sent = sut.EmailSender.Sent.Should().ContainSingle().Subject;
        sent.To.Should().Be("owner@example.com");
        sent.Body.Should().Contain(business.Name);
        sent.Body.Should().Contain($"/businesses/{business.Id}/banking");
    }

    [Fact]
    public async Task DoesNotEmailForAFailureYoungerThan24Hours()
    {
        var sut = BuildSut();
        SeedBrokenBusiness(sut, TimeSpan.FromHours(3));

        await sut.Job.Execute();

        sut.EmailSender.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task DoesNotEmailAHealthyBusiness()
    {
        var sut = BuildSut();
        var business = SeedBrokenBusiness(sut, TimeSpan.FromHours(30));
        business.ClearBankingSyncFailure();

        await sut.Job.Execute();

        sut.EmailSender.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task StampsTheEmailSoTheNextRunDoesNotRepeatIt()
    {
        var sut = BuildSut();
        var business = SeedBrokenBusiness(sut, TimeSpan.FromHours(25));

        await sut.Job.Execute();
        await sut.Job.Execute();

        sut.EmailSender.Sent.Should().ContainSingle();
        business.BankingFailureEmailSentAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SkipsABusinessWhoseTenantHasNoConfirmedEmail()
    {
        var sut = BuildSut();
        SeedBrokenBusiness(sut, TimeSpan.FromHours(25), ownerEmail: null);

        await sut.Job.Execute();

        sut.EmailSender.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task IncludesTheConsecutiveFailureCount()
    {
        var sut = BuildSut();
        SeedBrokenBusiness(sut, TimeSpan.FromHours(25), failureCount: 4);

        await sut.Job.Execute();

        sut.EmailSender.Sent.Single().Body.Should().Contain("4");
    }

    /// <summary>
    /// Both failure jobs used to build their own plain-text body and convert
    /// newlines to <c>&lt;br/&gt;</c>, which meant an unescaped business name
    /// went straight into the HTML. They now render through the shared branded
    /// template; this is the guard against that path coming back.
    /// </summary>
    [Fact]
    public async Task EmailUsesTheBrandedLayoutAndEscapesTheBusinessName()
    {
        var sut = BuildSut();
        var b = SeedBrokenBusiness(sut, TimeSpan.FromHours(25));
        typeof(Business).GetProperty(nameof(Business.Name))!
            .SetValue(b, """<script>alert(1)</script>""");

        await sut.Job.Execute();

        var body = sut.EmailSender.Sent.Should().ContainSingle().Subject.Body;
        body.Should().Contain("roivo-cta", "the branded layout renders a CTA button");
        body.Should().NotContain("<script>");
        body.Should().Contain("&lt;script&gt;");
    }
}
