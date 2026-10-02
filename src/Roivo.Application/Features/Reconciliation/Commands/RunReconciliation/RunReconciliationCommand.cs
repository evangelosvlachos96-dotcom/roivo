namespace Roivo.Application.Features.Reconciliation.Commands.RunReconciliation;

/// <summary>
/// Reconciles a business over a date window.
/// </summary>
/// <remarks>
/// <c>BypassTenantScope</c> is set only by the nightly cron, which runs with no
/// signed-in user and therefore no ambient tenant. Request-scoped callers must
/// leave it false.
/// </remarks>
public sealed record RunReconciliationCommand(
    Guid BusinessId,
    DateOnly From,
    DateOnly To,
    bool BypassTenantScope = false);
