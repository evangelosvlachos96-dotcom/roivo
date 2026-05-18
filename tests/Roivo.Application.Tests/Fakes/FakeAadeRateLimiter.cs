using Roivo.Application.Abstractions.Aade;

namespace Roivo.Application.Tests.Fakes;

public sealed class FakeAadeRateLimiter : IAadeRateLimiter
{
    public bool AllowNext { get; set; } = true;
    public List<Guid> Acquired { get; } = new();

    public Task<bool> TryAcquireValidationSlotAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        Acquired.Add(businessId);
        return Task.FromResult(AllowNext);
    }
}
