namespace Roivo.Core.Domain.Enums;

/// <summary>
/// What a tenant has paid for.
/// </summary>
/// <remarks>
/// Ordered deliberately: an entitlement check is a <c>&gt;=</c> against the tier
/// a feature requires, so a new tier only has to be inserted at the right rank
/// for every existing gate to keep meaning what it meant.
/// </remarks>
public enum PlanTier
{
    /// <summary>Free. Everything the product does without a subscription.</summary>
    Starter = 0,

    /// <summary>Paid. Longer forecasting, saved dashboards, custom report ranges.</summary>
    Pro = 1,

    /// <summary>Everything, including the 90-day forecast.</summary>
    Premium = 2,
}
