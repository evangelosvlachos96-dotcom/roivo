using Microsoft.Extensions.Caching.Memory;
using Roivo.Application.Abstractions.Aade;

namespace Roivo.Aade;

/// <summary>
/// Per-process rate limiter — 5 validation attempts per business per hour.
/// Sliding window via <see cref="IMemoryCache"/>.
///
/// Limitations (acceptable for M4, queue for post-MVP):
/// - State is in-memory; restarts reset all counters.
/// - Multi-instance deploys lose the per-business ceiling — each instance
///   counts independently. Distributed limiting via Redis is in later.md.
/// </summary>
public sealed class InMemoryAadeRateLimiter : IAadeRateLimiter
{
    private const int MaxAttemptsPerHour = 5;

    private readonly IMemoryCache _cache;

    public InMemoryAadeRateLimiter(IMemoryCache cache)
    {
        ArgumentNullException.ThrowIfNull(cache);
        _cache = cache;
    }

    public Task<bool> TryAcquireValidationSlotAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        var key = $"aade:validate:{businessId}";
        var attempts = _cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return 0;
        });

        if (attempts >= MaxAttemptsPerHour)
            return Task.FromResult(false);

        _cache.Set(key, attempts + 1, TimeSpan.FromHours(1));
        return Task.FromResult(true);
    }
}
