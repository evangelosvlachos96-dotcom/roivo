using Roivo.Core.Domain.Enums;

namespace Roivo.Application.Abstractions;

/// <summary>
/// The single place that answers "is this tenant allowed to do that".
/// </summary>
/// <remarks>
/// <para>
/// Deliberately one seam. There is no billing yet and every tenant is on
/// <see cref="PlanTier.Starter"/>, but the gates are real and the pages call
/// them — so when a payment provider is wired up, only the implementation
/// behind this interface changes and every gated feature follows.
/// </para>
/// <para>
/// Features name a capability rather than a tier, so re-pricing is a change to
/// one table in the implementation rather than a search across the UI for
/// everywhere "Pro" was hard-coded.
/// </para>
/// </remarks>
public interface IEntitlementService
{
    Task<PlanTier> GetCurrentPlanAsync(CancellationToken cancellationToken = default);

    Task<bool> CanAccessFeatureAsync(string featureKey, CancellationToken cancellationToken = default);
}

/// <summary>Capability keys for <see cref="IEntitlementService"/>.</summary>
public static class Features
{
    public const string ForecastDays60 = "forecast.60days";
    public const string ForecastDays90 = "forecast.90days";
    public const string DashboardCustomizationSave = "dashboard.customization.save";
    public const string CustomReportRange = "reports.custom_range";
    public const string CalendarNotes = "obligations.calendar_notes";
}
