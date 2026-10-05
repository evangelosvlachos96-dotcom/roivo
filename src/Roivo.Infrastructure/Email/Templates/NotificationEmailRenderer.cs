using Microsoft.Extensions.Configuration;
using Roivo.Application.Abstractions;
using Roivo.Core.Domain.Enums;
using Roivo.Resources;

namespace Roivo.Infrastructure.Email.Templates;

/// <summary>
/// Entry point to the email templates. Resolves the public base URL once and
/// renders either a real model or a placeholder sample.
/// </summary>
/// <remarks>
/// <para>
/// The Greek copy lives in <c>Roivo.Resources.Notifications</c> and is read
/// directly here. That is layer-legal: <c>Roivo.Infrastructure.csproj</c>
/// already references <c>Roivo.Resources</c> (the AADE and banking failure jobs
/// render user-facing bodies too), and the rule in CODING_STANDARDS forbids the
/// reference only from <c>Application</c>, <c>Core</c>, <c>Aade</c> and
/// <c>Banking</c>. The Application layer reaches these templates through
/// <c>INotificationEmailDispatcher</c>, so it never sees a Greek string.
/// </para>
/// <para>
/// Every entry point takes the recipient's language explicitly and defaults it
/// to Greek. Callers running inside a web request can pass
/// <see cref="Strings.IsEnglish"/>; the Hangfire jobs, whose worker threads
/// carry the machine culture rather than the recipient's, must pass the
/// language they looked up with the recipient. See <see cref="EmailCulture"/>.
/// </para>
/// <para>
/// Registered as a scoped or singleton service; it holds no mutable state.
/// </para>
/// </remarks>
public sealed class NotificationEmailRenderer
{
    private readonly IClock _clock;

    public NotificationEmailRenderer(IConfiguration configuration, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(clock);

        _clock = clock;
        BaseUrl = EmailHtml.NormalizeBaseUrl(configuration["App:PublicBaseUrl"]);
    }

    /// <summary>Public origin every link in a rendered email is built from.</summary>
    public string BaseUrl { get; }

    public RenderedEmail Render(DailyDigestEmail.Model model, bool english = false)
        => DailyDigestEmail.Render(model, BaseUrl, notice: null, english: english);

    public RenderedEmail Render(TaxReminderEmail.Model model, bool english = false)
        => TaxReminderEmail.Render(model, BaseUrl, notice: null, english: english);

    public RenderedEmail Render(CashflowAlertEmail.Model model, bool english = false)
        => CashflowAlertEmail.Render(model, BaseUrl, notice: null, english: english);

    public RenderedEmail Render(SyncFailureEmail.Model model, bool english = false)
        => SyncFailureEmail.Render(model, BaseUrl, notice: null, english: english);

    public RenderedEmail Render(WeeklyReconciliationEmail.Model model, bool english = false)
        => WeeklyReconciliationEmail.Render(model, BaseUrl, notice: null, english: english);

    public RenderedEmail Render(EmailConfirmationEmail.Model model, bool english = false)
        => EmailConfirmationEmail.Render(model, BaseUrl, english);

    public RenderedEmail Render(PasswordResetEmail.Model model, bool english = false)
        => PasswordResetEmail.Render(model, BaseUrl, english);

    /// <summary>
    /// Renders <paramref name="kind"/> with invented figures so a user can see
    /// the layout on demand. The subject carries a visible test marker and the
    /// body a banner, so a sample is never mistaken for a real alert.
    /// </summary>
    public RenderedEmail RenderSample(
        NotificationKind kind,
        Guid businessId,
        string businessName,
        bool english = false)
    {
        // Scoped here rather than only inside the template: the sample's own
        // placeholder strings — the notice banner, the stand-in business name,
        // the failure reason — are resolved before any template is called.
        using var culture = EmailCulture.For(english);

        var name = string.IsNullOrWhiteSpace(businessName) ? Notifications.Sample_BusinessName : businessName;
        var today = _clock.Today;
        var notice = Notifications.Sample_Notice;

        var rendered = kind switch
        {
            NotificationKind.DailyDigest => DailyDigestEmail.Render(
                new DailyDigestEmail.Model(today, [new DailyDigestEmail.Row(businessId, name, 12, 31, 4)]),
                BaseUrl, notice, english),

            NotificationKind.TaxReminder => TaxReminderEmail.Render(
                new TaxReminderEmail.Model(businessId, name, 7,
                [
                    new TaxReminderEmail.Row(Notifications.Kind_TaxReminder, today.AddDays(7), 1240.55m),
                ]),
                BaseUrl, notice, english),

            NotificationKind.CashflowAlert => CashflowAlertEmail.Render(
                new CashflowAlertEmail.Model(businessId, name, -820.40m, today.AddDays(23), 0m),
                BaseUrl, notice, english),

            NotificationKind.SyncFailure => SyncFailureEmail.Render(
                new SyncFailureEmail.Model(
                    businessId, name, Notifications.Kind_SyncFailure,
                    Aade.FailureReasonGeneric, _clock.UtcNow.AddHours(-26), "aade"),
                BaseUrl, notice, english),

            NotificationKind.WeeklyReconciliation => WeeklyReconciliationEmail.Render(
                new WeeklyReconciliationEmail.Model(
                    businessId, name, today.AddDays(-7), today.AddDays(-1),
                    0.82m, 41, 6, 3, 5),
                BaseUrl, notice, english),

            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown notification kind."),
        };

        return rendered with { Subject = $"{Notifications.TestSubjectPrefix} {rendered.Subject}" };
    }
}
