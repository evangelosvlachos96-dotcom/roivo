using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
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
    /// Mirrors <c>EmailLayout.CtaClass</c>, which is internal. If the layout's
    /// marker changes, these tests fail loudly rather than quietly asserting
    /// nothing.
    /// </summary>
    private const string CtaClass = "roivo-cta";

    /// <summary>
    /// A trade name that would break the markup if interpolated raw. Real AADE
    /// counterparty names do contain angle brackets and ampersands.
    /// </summary>
    private const string HostileName = """<script>alert(1)</script>"&""";

    /// <summary>
    /// Shaped like a real Identity token: base64 with the <c>+ / =</c> already
    /// percent-escaped by <c>QueryHelpers.AddQueryString</c>.
    /// </summary>
    private const string Token = "CfDJ8NrAkS4c%2BZxLn3BvQ%2FkHu8w%3D%3D";

    private const string ConfirmUrl =
        $"{BaseUrl}/Account/ConfirmEmail?userId=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee&token={Token}";

    private const string CleanResetUrl =
        $"{BaseUrl}/Account/ResetPassword?email=owner%40example.gr&token={Token}";

    private static readonly Guid BusinessId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    private static readonly DateOnly Today = new(2026, 10, 3);

    private static readonly bool[] BothLanguages = [false, true];

    private static NotificationEmailRenderer BuildRenderer()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["App:PublicBaseUrl"] = BaseUrl })
            .Build();

        return new NotificationEmailRenderer(config, new FixedClock(Today));
    }

    /// <summary>One rendered email per template, with the URL its CTA must point at.</summary>
    /// <remarks>
    /// The reset email has no name field of its own — its only interpolated
    /// value is the URL — so the hostile string is fed through the URL instead,
    /// which is the surface that email actually has.
    /// </remarks>
    private static IEnumerable<(string Template, RenderedEmail Email, string CtaUrl)> AllTemplates(
        string businessName, bool english)
    {
        yield return (
            nameof(DailyDigestEmail),
            DailyDigestEmail.Render(
                new DailyDigestEmail.Model(Today, [new DailyDigestEmail.Row(BusinessId, businessName, 3, 9, 2)]),
                BaseUrl, notice: null, english: english),
            $"{BaseUrl}/dashboard");

        yield return (
            nameof(TaxReminderEmail),
            TaxReminderEmail.Render(
                new TaxReminderEmail.Model(BusinessId, businessName, 7,
                    [new TaxReminderEmail.Row(businessName, Today.AddDays(7), 1200m)]),
                BaseUrl, notice: null, english: english),
            $"{BaseUrl}/businesses/{BusinessId}/tax-calendar");

        yield return (
            nameof(CashflowAlertEmail),
            CashflowAlertEmail.Render(
                new CashflowAlertEmail.Model(BusinessId, businessName, -500m, Today.AddDays(12), 0m),
                BaseUrl, notice: null, english: english),
            $"{BaseUrl}/businesses/{BusinessId}/cashflow");

        yield return (
            nameof(SyncFailureEmail),
            SyncFailureEmail.Render(
                new SyncFailureEmail.Model(
                    BusinessId, businessName, businessName, businessName,
                    new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc), "aade"),
                BaseUrl, notice: null, english: english),
            $"{BaseUrl}/businesses/{BusinessId}/aade");

        yield return (
            nameof(WeeklyReconciliationEmail),
            WeeklyReconciliationEmail.Render(
                new WeeklyReconciliationEmail.Model(
                    BusinessId, businessName, Today.AddDays(-7), Today.AddDays(-1), 0.75m, 10, 2, 1, 3),
                BaseUrl, notice: null, english: english),
            $"{BaseUrl}/businesses/{BusinessId}/reconciliation");

        yield return (
            nameof(EmailConfirmationEmail),
            EmailConfirmationEmail.Render(
                new EmailConfirmationEmail.Model(businessName, ConfirmUrl), BaseUrl, english),
            ConfirmUrl);

        var resetUrl = $"{BaseUrl}/Account/ResetPassword?email={businessName}&token={Token}";
        yield return (
            nameof(PasswordResetEmail),
            PasswordResetEmail.Render(new PasswordResetEmail.Model(resetUrl), BaseUrl, english),
            resetUrl);
    }

    // -------------------------------------------------- sync-failure details

    private static SyncFailureEmail.Model SyncModel(int? failedAttempts) =>
        new(BusinessId, "Acme", "AADE myDATA", "Invalid credentials",
            new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc), "aade",
            failedAttempts);

    /// <summary>
    /// Asserted against the literal Greek label rather than a resource read:
    /// the render pins its own culture, so reading the property here would
    /// resolve against the test host's culture and compare the wrong string.
    /// </summary>
    private const string AttemptsLabel = "Αποτυχημένες προσπάθειες";

    private const string ReasonLabel = "Αιτία";

    /// <summary>
    /// Banking counts consecutive failures and AADE does not, so the row is
    /// optional. A null count must omit the row rather than render "0", which
    /// would read as "it has never failed" in the one email that exists to say
    /// the opposite.
    /// </summary>
    [Fact]
    public void SyncFailureEmail_OmitsTheAttemptsRow_WhenTheCountIsUnknown()
    {
        var html = SyncFailureEmail.Render(SyncModel(null), BaseUrl).HtmlBody;

        html.Should().NotContain(AttemptsLabel);
        html.Should().Contain(ReasonLabel,
            "the rows that do not depend on the count must still render");
    }

    [Fact]
    public void SyncFailureEmail_RendersTheAttemptsRow_WhenTheCountIsKnown()
    {
        var html = SyncFailureEmail.Render(SyncModel(7), BaseUrl).HtmlBody;

        html.Should().Contain(AttemptsLabel);
        html.Should().Contain(">7<");
    }

    /// <summary>
    /// Zero is a real reading the caller can produce, and it must not be
    /// mistaken for "unknown" and dropped.
    /// </summary>
    [Fact]
    public void SyncFailureEmail_RendersTheAttemptsRow_WhenTheCountIsZero()
    {
        var html = SyncFailureEmail.Render(SyncModel(0), BaseUrl).HtmlBody;

        html.Should().Contain(AttemptsLabel);
    }

    // ------------------------------------------------------------- bilingual

    [Fact]
    public void EveryTemplateRendersInBothLanguagesAndTheTwoBodiesDiffer()
    {
        var greek = AllTemplates("Acme", english: false).ToList();
        var english = AllTemplates("Acme", english: true).ToList();

        greek.Should().HaveCount(7);
        english.Should().HaveCount(7);

        for (var i = 0; i < greek.Count; i++)
        {
            greek[i].Email.HtmlBody.Should().NotBeNullOrWhiteSpace();
            english[i].Email.HtmlBody.Should().NotBeNullOrWhiteSpace();
            english[i].Email.HtmlBody.Should().NotBe(greek[i].Email.HtmlBody,
                $"{greek[i].Template} must not send the same copy to both languages");
            english[i].Email.Subject.Should().NotBeNullOrWhiteSpace();
        }
    }

    /// <summary>
    /// The failure mode this guards: a Hangfire worker thread whose culture is
    /// Greek rendering for an English recipient (or the reverse). The language
    /// argument must beat the ambient culture in both directions.
    /// </summary>
    [Theory]
    [InlineData("el-GR")]
    [InlineData("en-IE")]
    [InlineData("de-DE")]
    public void TheLanguageArgumentBeatsTheAmbientCulture(string ambientCulture)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(ambientCulture);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(ambientCulture);

            var greek = DailyDigestEmail.Render(
                new DailyDigestEmail.Model(Today, [new DailyDigestEmail.Row(BusinessId, "Acme", 3, 9, 2)]),
                BaseUrl, notice: null, english: false);

            var english = DailyDigestEmail.Render(
                new DailyDigestEmail.Model(Today, [new DailyDigestEmail.Row(BusinessId, "Acme", 3, 9, 2)]),
                BaseUrl, notice: null, english: true);

            greek.HtmlBody.Should().Contain("Η ημερήσια σύνοψή σου");
            greek.HtmlBody.Should().NotContain("Your daily digest");

            english.HtmlBody.Should().Contain("Your daily digest");
            english.HtmlBody.Should().NotContain("Η ημερήσια σύνοψή σου");
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void RenderingRestoresTheCallersCulture()
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("el-GR");

            _ = PasswordResetEmail.Render(new PasswordResetEmail.Model(CleanResetUrl), BaseUrl, english: true);

            CultureInfo.CurrentUICulture.Name.Should().Be("el-GR",
                "a render must not leak its language into the thread that called it");
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void TheAccountEmailsCarryTranslatedCopyNotJustTranslatedChrome()
    {
        var greek = EmailConfirmationEmail.Render(
            new EmailConfirmationEmail.Model("Maria", ConfirmUrl), BaseUrl, english: false);
        var english = EmailConfirmationEmail.Render(
            new EmailConfirmationEmail.Model("Maria", ConfirmUrl), BaseUrl, english: true);

        greek.Subject.Should().Be("Επιβεβαίωση email — Roivo");
        english.Subject.Should().Be("Confirm your email — Roivo");

        greek.HtmlBody.Should().Contain("Καλωσόρισες στο Roivo").And.Contain("Γεια σου Maria,");
        english.HtmlBody.Should().Contain("Welcome to Roivo").And.Contain("Hi Maria,");

        var greekReset = PasswordResetEmail.Render(
            new PasswordResetEmail.Model(CleanResetUrl), BaseUrl, english: false);
        var englishReset = PasswordResetEmail.Render(
            new PasswordResetEmail.Model(CleanResetUrl), BaseUrl, english: true);

        greekReset.Subject.Should().Be("Επαναφορά κωδικού — Roivo");
        englishReset.Subject.Should().Be("Reset your password — Roivo");
        englishReset.HtmlBody.Should().Contain("Reset your password");
    }

    [Fact]
    public void MoneyFollowsTheRecipientsLanguageSoAmountsAreNotMisread()
    {
        var greek = CashflowAlertEmail.Render(
            new CashflowAlertEmail.Model(BusinessId, "Acme", 1234.56m, Today, 0m),
            BaseUrl, notice: null, english: false);

        var english = CashflowAlertEmail.Render(
            new CashflowAlertEmail.Model(BusinessId, "Acme", 1234.56m, Today, 0m),
            BaseUrl, notice: null, english: true);

        greek.HtmlBody.Should().Contain("1.234,56");
        english.HtmlBody.Should().Contain("1,234.56");

        // Dates stay day-first in both languages: these are Greek businesses
        // whichever language they read the product in.
        greek.HtmlBody.Should().Contain("03/10/2026");
        english.HtmlBody.Should().Contain("03/10/2026");
    }

    // ------------------------------------------------------------- escaping

    [Fact]
    public void EveryTemplateEscapesInterpolatedTextInBothLanguages()
    {
        foreach (var english in BothLanguages)
        {
            foreach (var (template, email, _) in AllTemplates(HostileName, english))
            {
                email.HtmlBody.Should().NotContain("<script>",
                    $"{template} must not let an angle bracket out of an interpolated value");
                email.HtmlBody.Should().NotContain("alert(1)</script>");
                email.HtmlBody.Should().Contain("&lt;script&gt;",
                    $"{template} should show the hostile text as literal characters");
                email.HtmlBody.Should().Contain("&amp;");
            }
        }
    }

    [Fact]
    public void EscapedMarkupStillHasBalancedTags()
    {
        foreach (var (template, email, _) in AllTemplates(HostileName, english: false))
        {
            // A swallowed element shows up as a tag-count mismatch long before
            // it shows up as a visibly broken email.
            CountOccurrences(email.HtmlBody, "<table")
                .Should().Be(CountOccurrences(email.HtmlBody, "</table>"), template);
            CountOccurrences(email.HtmlBody, "<tr")
                .Should().Be(CountOccurrences(email.HtmlBody, "</tr>"), template);
            CountOccurrences(email.HtmlBody, "<td")
                .Should().Be(CountOccurrences(email.HtmlBody, "</td>"), template);
            CountOccurrences(email.HtmlBody, "<a ")
                .Should().Be(CountOccurrences(email.HtmlBody, "</a>"), template);
        }
    }

    [Fact]
    public void SubjectsAreLeftUnescapedBecauseMailClientsRenderThemAsText()
    {
        var email = CashflowAlertEmail.Render(
            new CashflowAlertEmail.Model(BusinessId, "Smith & Jones", -1m, Today, 0m), BaseUrl);

        email.Subject.Should().Contain("Smith & Jones").And.NotContain("&amp;");
    }

    // ------------------------------------------------------------------ CTA

    [Fact]
    public void EveryTemplateHasExactlyOneCtaAnchorPointingAtTheExpectedUrl()
    {
        foreach (var english in BothLanguages)
        {
            foreach (var (template, email, expectedUrl) in AllTemplates("Acme", english))
            {
                CountOccurrences(email.HtmlBody, $"class=\"{CtaClass}\"").Should().Be(1,
                    $"{template} must offer the recipient exactly one action");

                var href = CtaHref(email.HtmlBody);

                // Asserted on the attribute, not on a substring of the body:
                // the footer link also starts with the base URL, so a naive
                // Contains would pass even with a broken button.
                href.Should().Be(WebUtility.HtmlEncode(expectedUrl), template);
                WebUtility.HtmlDecode(href).Should().Be(expectedUrl, template);
            }
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
        foreach (var english in BothLanguages)
        {
            var email = BuildRenderer().RenderSample(kind, BusinessId, "Acme", english);

            CtaHref(email.HtmlBody).Should().StartWith(BaseUrl).And.EndWith(expectedPath);
        }
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

        CtaHref(email.HtmlBody).Should().Be($"{BaseUrl}/dashboard");
        email.HtmlBody.Should().Contain("Acme").And.Contain("Beta");
    }

    [Fact]
    public void AnEmptyDigestStillRendersWithoutADetailTable()
    {
        var email = DailyDigestEmail.Render(new DailyDigestEmail.Model(Today, []), BaseUrl);

        email.HtmlBody.Should().NotBeNullOrWhiteSpace();
        CtaHref(email.HtmlBody).Should().Be($"{BaseUrl}/dashboard");
    }

    // -------------------------------------------------------- account emails

    [Fact]
    public void TheConfirmationAndResetCtasCarryTheTokenUrlUnmodified()
    {
        foreach (var english in BothLanguages)
        {
            var confirmation = EmailConfirmationEmail.Render(
                new EmailConfirmationEmail.Model("Maria", ConfirmUrl), BaseUrl, english);

            var reset = PasswordResetEmail.Render(
                new PasswordResetEmail.Model(CleanResetUrl), BaseUrl, english);

            // The only transformation the template is allowed to apply is HTML
            // attribute escaping, which turns the query separator into &amp;
            // and which every client decodes back before navigating.
            WebUtility.HtmlDecode(CtaHref(confirmation.HtmlBody)).Should().Be(ConfirmUrl);
            WebUtility.HtmlDecode(CtaHref(reset.HtmlBody)).Should().Be(CleanResetUrl);

            // The token itself must survive character for character; a single
            // re-encoded '%' makes the link unusable.
            confirmation.HtmlBody.Should().Contain(Token);
            reset.HtmlBody.Should().Contain(Token);

            // And the copy-me fallback must carry the same URL, as plain text
            // rather than as a second anchor.
            confirmation.HtmlBody.Should().Contain(WebUtility.HtmlEncode(ConfirmUrl));
            reset.HtmlBody.Should().Contain(WebUtility.HtmlEncode(CleanResetUrl));
        }
    }

    [Fact]
    public void TheAccountEmailsRefuseToRenderWithoutALink()
    {
        var confirmation = () => EmailConfirmationEmail.Render(
            new EmailConfirmationEmail.Model("Maria", "  "), BaseUrl);
        var reset = () => PasswordResetEmail.Render(new PasswordResetEmail.Model(""), BaseUrl);

        // A confirmation email with no working link is worse than no email:
        // the user waits for one that can never arrive.
        confirmation.Should().Throw<ArgumentException>();
        reset.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TheAccountEmailsDoNotClaimToBeNotificationPreferences()
    {
        var confirmation = EmailConfirmationEmail.Render(
            new EmailConfirmationEmail.Model("Maria", ConfirmUrl), BaseUrl, english: true);

        confirmation.HtmlBody.Should().Contain("a Roivo account was created with this email address");
        confirmation.HtmlBody.Should().NotContain("notifications turned on");
    }

    // ------------------------------------------------------------ structure

    [Fact]
    public void NoTemplateCarriesAStyleBlockBecauseGmailStripsThem()
    {
        foreach (var english in BothLanguages)
        {
            foreach (var (template, email, _) in AllTemplates("Acme", english))
            {
                email.HtmlBody.Should().NotContain("<style", $"{template} must keep all CSS inline");
                email.HtmlBody.Should().NotContain("</style>", template);
                email.HtmlBody.Should().NotContain("@media", template);

                // No remote or embedded image either: both cost us a blocked or
                // a bloated send, so the brand mark is markup.
                email.HtmlBody.Should().NotContain("<img", template);
                email.HtmlBody.Should().NotContain("base64", template);
            }
        }
    }

    [Fact]
    public void EveryTemplateCarriesTheBrandedShellAndTheLegalFooter()
    {
        foreach (var english in BothLanguages)
        {
            foreach (var (template, email, _) in AllTemplates("Acme", english))
            {
                email.HtmlBody.Should().Contain(">roivo<", template);
                email.HtmlBody.Should().Contain("Cashflow Intelligence", template);
                email.HtmlBody.Should().Contain("max-width:600px", template);
                email.HtmlBody.Should().Contain("#0B1F3A", template);
                email.HtmlBody.Should().Contain("linear-gradient(135deg, #1E88E5, #2DD4BF)", template);
                email.HtmlBody.Should().Contain("Inter,'Helvetica Neue',Helvetica,Arial,sans-serif", template);

                // CAN-SPAM and GDPR both expect a physical address. It is a
                // placeholder until someone fills it in — which is the point of
                // asserting on it.
                email.HtmlBody.Should().Contain("[COMPANY_ADDRESS]", template);

                email.HtmlBody.Should().Contain("class=\"roivo-footer-link\"", template);
                email.Subject.Should().NotBeNullOrWhiteSpace(template);
            }
        }
    }

    [Fact]
    public void TheNotificationTemplatesLinkToTheirBusinessSettingsPage()
    {
        foreach (var (template, email, _) in AllTemplates("Acme", english: false).Take(5))
        {
            email.HtmlBody.Should().Contain(
                $"href=\"{BaseUrl}/businesses/{BusinessId}/settings/notifications\"", template);
        }
    }

    // --------------------------------------------------------- test samples

    [Fact]
    public void SampleRendersMarkedAsTestsWithAPlaceholderName()
    {
        var email = BuildRenderer().RenderSample(NotificationKind.TaxReminder, BusinessId, "");

        email.Subject.Should().StartWith("[Δοκιμή]");
        email.HtmlBody.Should().Contain("Δείγμα Επιχείρησης");

        var englishSample = BuildRenderer().RenderSample(
            NotificationKind.TaxReminder, BusinessId, "", english: true);

        englishSample.Subject.Should().StartWith("[Test]");
        englishSample.HtmlBody.Should().Contain("Sample Business");
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

    [Fact]
    public void TheRendererExposesTheTwoAccountTemplatesToo()
    {
        var renderer = BuildRenderer();

        var confirmation = renderer.Render(
            new EmailConfirmationEmail.Model("Maria", ConfirmUrl), english: true);
        var reset = renderer.Render(new PasswordResetEmail.Model(CleanResetUrl));

        confirmation.Subject.Should().Be("Confirm your email — Roivo");
        reset.Subject.Should().Be("Επαναφορά κωδικού — Roivo");
    }

    /// <summary>The href of the single CTA anchor, still HTML-escaped.</summary>
    private static string CtaHref(string html)
    {
        var match = Regex.Match(
            html,
            $"class=\"{CtaClass}\"\\s+href=\"(?<href>[^\"]*)\"",
            RegexOptions.None,
            TimeSpan.FromSeconds(2));

        match.Success.Should().BeTrue("the layout must render exactly one marked CTA anchor");
        return match.Groups["href"].Value;
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
