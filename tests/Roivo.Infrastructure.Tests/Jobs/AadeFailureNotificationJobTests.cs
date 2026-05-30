using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Roivo.Core.Domain.Entities;
using Roivo.Infrastructure.Jobs;
using Roivo.Infrastructure.Tests.Fakes;

namespace Roivo.Infrastructure.Tests.Jobs;

public class AadeFailureNotificationJobTests
{
    private const string ValidAfm = "094014201";

    private sealed record Sut(
        AadeFailureNotificationJob Job,
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
        var job = new AadeFailureNotificationJob(businesses, email, config, NullLogger<AadeFailureNotificationJob>.Instance);
        return new Sut(job, businesses, email);
    }

    /// <summary>
    /// Seeds an active business attached to a tenant whose primary email is known
    /// to the fake repository, then forces the failure-state fields onto the
    /// entity via the rich-domain methods. Optionally rewrites the start time
    /// via reflection so the test can simulate a stale failure.
    /// </summary>
    private static Business SeedBrokenBusiness(
        Sut sut,
        TimeSpan failureAge,
        string ownerEmail = "owner@example.com")
    {
        var tenantId = Guid.NewGuid();
        var b = Business.Create("Acme", ValidAfm, null, null);
        b.TenantId = tenantId;
        b.RecordAadeSyncFailure("InvalidCredentials");

        // Backdate the failure start so the 24h window check fires. Private
        // setter is intentional; tests reach in to set up a precondition that
        // would otherwise need a fake clock.
        var prop = typeof(Business).GetProperty(nameof(Business.AadeLastFailureAt))!;
        prop.SetValue(b, DateTime.UtcNow - failureAge);

        sut.BusinessRepo.Store[b.Id] = b;
        sut.BusinessRepo.TenantPrimaryEmails[tenantId] = ownerEmail;
        return b;
    }

    [Fact]
    public async Task SendsEmailForBusinessesBrokenOver24Hours()
    {
        var sut = BuildSut();
        var b = SeedBrokenBusiness(sut, TimeSpan.FromHours(25));

        await sut.Job.Execute();

        sut.EmailSender.Sent.Should().ContainSingle()
            .Which.To.Should().Be("owner@example.com");
        sut.EmailSender.Sent[0].Body.Should().Contain(b.Name);
        sut.EmailSender.Sent[0].Body.Should().Contain("https://test.roivo.gr/businesses/" + b.Id + "/aade");
    }

    [Fact]
    public async Task DoesNotSendForBusinessesBrokenLessThan24Hours()
    {
        var sut = BuildSut();
        SeedBrokenBusiness(sut, TimeSpan.FromHours(2));

        await sut.Job.Execute();

        sut.EmailSender.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task DoesNotSendDuplicateEmails()
    {
        var sut = BuildSut();
        var b = SeedBrokenBusiness(sut, TimeSpan.FromHours(48));
        b.RecordAadeFailureEmailSent();

        await sut.Job.Execute();

        sut.EmailSender.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task RecordsEmailSentAfterSending()
    {
        var sut = BuildSut();
        var b = SeedBrokenBusiness(sut, TimeSpan.FromHours(25));

        await sut.Job.Execute();

        sut.BusinessRepo.Store[b.Id].AadeFailureEmailSentAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SkipsWhenTenantHasNoOwnerEmail()
    {
        var sut = BuildSut();
        var b = SeedBrokenBusiness(sut, TimeSpan.FromHours(25));
        sut.BusinessRepo.TenantPrimaryEmails[b.TenantId] = null;

        await sut.Job.Execute();

        sut.EmailSender.Sent.Should().BeEmpty();
        sut.BusinessRepo.Store[b.Id].AadeFailureEmailSentAt.Should().BeNull();
    }
}
