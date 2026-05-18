namespace Roivo.Application.Abstractions.Aade;

/// <summary>
/// Rate-limits credential-validation attempts to defeat AFM enumeration. The
/// M4 implementation is per-process (in-memory); distributed limiting via Redis
/// is queued for post-MVP.
/// </summary>
public interface IAadeRateLimiter
{
    /// <summary>
    /// Returns <c>true</c> if the caller is allowed to attempt a validation,
    /// <c>false</c> if the per-business rate ceiling has been reached.
    /// </summary>
    Task<bool> TryAcquireValidationSlotAsync(Guid businessId, CancellationToken cancellationToken = default);
}
