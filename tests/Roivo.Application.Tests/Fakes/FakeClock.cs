using Roivo.Application.Abstractions;

namespace Roivo.Application.Tests.Fakes;

/// <summary>A clock frozen at an instant the test chooses.</summary>
public sealed class FakeClock : IClock
{
    public FakeClock(DateTime utcNow)
    {
        UtcNow = utcNow;
    }

    public FakeClock(DateOnly today)
        : this(today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))
    {
    }

    public DateTime UtcNow { get; set; }

    public DateOnly Today => DateOnly.FromDateTime(UtcNow);

    public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);
}
