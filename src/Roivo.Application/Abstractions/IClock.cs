namespace Roivo.Application.Abstractions;

/// <summary>
/// The current time, as an injectable dependency.
/// </summary>
/// <remarks>
/// Everything that makes a decision about "now" goes through this: whether a
/// tax obligation is overdue, which 90-day window a forecast covers, what the
/// default reconciliation range is. Reading the ambient clock directly spread
/// three different notions of today across the app — pages using
/// <c>DateTime.Today</c> (server local) while handlers used
/// <c>DateTime.UtcNow</c>, so between local midnight and 03:00 Athens the same
/// obligation was overdue on one screen and pending on another. It also makes
/// date-boundary behaviour testable without waiting for a particular day.
/// </remarks>
public interface IClock
{
    /// <summary>The current instant in UTC.</summary>
    DateTime UtcNow { get; }

    /// <summary>Today's date in UTC.</summary>
    DateOnly Today { get; }
}

/// <summary>The real clock. Registered as a singleton; it holds no state.</summary>
public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;

    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}
