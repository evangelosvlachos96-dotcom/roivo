using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Enums;
using Roivo.Infrastructure.Email.Templates;

namespace Roivo.Infrastructure.Tests.Email;

public class NotificationEmailTemplateTests
{
    private const string BaseUrl = "https://test.roivo.gr";

    /// <summary>
    /// A trade name that would break the markup if interpolated raw. Real AADE
    /// counterparty names do contain angle brackets and ampersands.
    /// </summary>
    private const string HostileName = """Smith & Jones <script>alert('x')</script> "Ltd" """;

    private static readonly Guid BusinessId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    private static readonly DateOnly Today = new(2026, 10, 3);

    private static NotificationEmailRenderer BuildRenderer()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["App:PublicBaseUrl"] = BaseUrl })
            .Build();

        return new NotificationEmailRenderer(config, new FixedClock(Today));
    }

    private static IEnumerable<RenderedEmail> AllTemplatesWith(string businessName)
    {
        yield return DailyDigestEmail.Render(
            new DailyDigestEmail.Model(Today, [new DailyDigestEmail.Row(BusinessId, businessName, 3, 9, 2)]),
            BaseUrl);

        yield return TaxReminderEmail.Render(
            new TaxReminderEmail.Model(BusinessId, businessName, 7,
                [new TaxReminderEmail.Row(businessName, Today.AddDays(7), 1200m)]),
            BaseUrl);

        yield return CashflowAlertEmail.Render(
            new CashflowAlertEmail.Model(BusinessId, businessName, -500m, Today.AddDays(12), 0m),
            BaseUrl);

        yield return SyncFailureEmail.Render(
            new SyncFailureEmail.Model(
                BusinessId, businessName, businessName, businessName,
                new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc), "aade"),
            BaseUrl);

        yield return WeeklyReconciliationEmail.Render(
            new WeeklyReconciliationEmail.Model(
                BusinessId, businessName, Today.AddDays(-7), Today.AddDays(-1), 0.75m, 10, 2, 1, 3),
            BaseUrl);
    }

    // ------------------------------------------------------------- escaping

    [Fact]
    public void EveryTemplateEscapesInterpolatedNames()
    {
        foreach (var email in AllTemplatesWith(HostileName))
        {
            email.HtmlBody.Should().NotContain("<script>",
                "an unescaped angle bracket in a trade name would break the markup");
            email.HtmlBody.Should().NotContain("alert('x')");
            email.HtmlBody.Should().Contain("&lt;script&gt;");
            email.HtmlBody.Should().Contain("Smith &amp; Jones");
        }
    }

    [Fact]
    public void EscapedMarkupStillHasBalancedTags()
    {
        var email = DailyDigestEmail.Render(
            new DailyDigestEmail.Model(Today, [new DailyDigestEmail.Row(BusinessId, HostileName, 1, 1, 1)]),
            BaseUrl);

        // A swallowed element shows up as a tag-count mismatch long before it
        // shows up as a visibly broken email.
        CountOccurrences(email.HtmlBody, "<table").Should().Be(CountOccurrences(email.HtmlBody, "</table>"));
        CountOccurrences(email.HtmlBody, "<tr").Should().Be(CountOccurrences(email.HtmlBody, "</tr>"));
        CountOccurrences(email.HtmlBody, "<td").Should().Be(CountOccurrences(email.HtmlBody, "</td>"));
    }

    [Fact]
    public void SubjectsAreLeftUnescapedBecauseMailClientsRenderThemAsText()
    {
        var email = CashflowAlertEmail.Render(
            new CashflowAlertEmail.Model(BusinessId, "Smith & Jones", -1m, Today, 0m), BaseUrl);

        email.Subject.Should().Contain("Smith & Jones").And.NotContain("&amp;");
    }

    // ------------------------------------------------------------ structure

    [Fact]
    public void EveryTemplateCarriesTheWordmarkAndAManageLink()
    {
        foreach (var email in AllTemplatesWith("Acme"))
        {
            email.HtmlBody.Should().Contain(">roivo<");
            email.HtmlBody.Should().Contain($"{BaseUrl}/businesses/{BusinessId}/settings/notifications");
            email.HtmlBody.Should().Contain("max-width:600px");
            // Email clients strip <style>, so there must not be one to strip.
            email.HtmlBody.Should().NotContain("<style");
            email.Subject.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Theory]
    [InlineData(NotificationKind.DailyDigest, "/dashboard")]
    [InlineData(NotificationKind.TaxReminder, "/tax-calendar")]
    [InlineData(NotificationKind.CashflowAlert, "/cashflow")]
    [InlineData(NotificationKind.SyncFailure, "/aade")]
    [InlineData(NotificationKind.WeeklyReconciliation, "/reconciliation")]
    public void EveryKindRendersACtaButtonLinkingIntoTheApp(NotificationKind kind, string expectedPath)
    {
        var email = BuildRenderer().RenderSample(kind, BusinessId, "Acme");

        email.HtmlBody.Should().Contain($"href=\"{BaseUrl}");
        email.HtmlBody.Should().Contain(expectedPath);
    }

    [Fact]
    public void TheDigestCtaGoesToTheDashboardBecauseItSpansBusinesses()
    {
        var email = DailyDigestEmail.Render(
            new DailyDigestEmail.Model(Today,
            [
                new DailyDigestEmail.Row(BusinessId, "Acme", 1, 2, 0),
                new DailyDigestEmail.Row(Guid.NewGuid(), "Beta", 0, 1, 1),
            ]),
            BaseUrl);

        email.HtmlBody.Should().Contain($"href=\"{BaseUrl}/dashboard\"");
        email.HtmlBody.Should().Contain("Acme").And.Contain("Beta");
    }

    [Fact]
    public void AnEmptyDigestStillRendersWithoutADetailTable()
    {
        var email = DailyDigestEmail.Render(new DailyDigestEmail.Model(Today, []), BaseUrl);

        email.HtmlBody.Should().NotBeNullOrWhiteSpace();
        email.HtmlBody.Should().Contain($"href=\"{BaseUrl}/dashboard\"");
    }

    // --------------------------------------------------------- test samples

    [Fact]
    public void SampleRendersMarkedAsTestsWithAPlaceholderName()
    {
        var email = BuildRenderer().RenderSample(NotificationKind.TaxReminder, BusinessId, "");

        email.Subject.Should().StartWith("[Δοκιμή]");
        email.HtmlBody.Should().Contain("Δείγμα Επιχείρησης");
    }

    [Fact]
    public void SampleEscapesABusinessNameFromTheUrlBar()
    {
        var email = BuildRenderer().RenderSample(NotificationKind.WeeklyReconciliation, BusinessId, HostileName);

        email.HtmlBody.Should().NotContain("<script>").And.Contain("&lt;script&gt;");
    }

    [Fact]
    public void RendererFallsBackToProductionWhenNoBaseUrlIsConfigured()
    {
        var renderer = new NotificationEmailRenderer(
            new ConfigurationBuilder().Build(), new FixedClock(Today));

        renderer.BaseUrl.Should().Be("https://roivo.gr");
    }

    [Fact]
    public void RendererTrimsATrailingSlashSoLinksAreNotDoubled()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["App:PublicBaseUrl"] = "https://test.roivo.gr/"
            })
            .Build();

        var email = new NotificationEmailRenderer(config, new FixedClock(Today))
            .RenderSample(NotificationKind.CashflowAlert, BusinessId, "Acme");

        email.HtmlBody.Should().NotContain("https://test.roivo.gr//");
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = haystack.IndexOf(needle, StringComparison.Ordinal);
        while (index >= 0)
        {
            count++;
            index = haystack.IndexOf(needle, index + needle.Length, StringComparison.Ordinal);
        }
        return count;
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateOnly today)
        {
            UtcNow = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        }

        public DateTime UtcNow { get; }

        public DateOnly Today => DateOnly.FromDateTime(UtcNow);
    }
}
