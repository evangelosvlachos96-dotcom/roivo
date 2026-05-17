namespace Roivo.Application.Abstractions;

/// <summary>
/// Resolves the current request's tenant identifier. Implementations source
/// the value from the request principal's <c>tenant_id</c> claim or equivalent.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// The current tenant identifier, or <see langword="null"/> if no tenant
    /// is in scope (e.g., anonymous request).
    /// </summary>
    Guid? CurrentTenantId { get; }
}
