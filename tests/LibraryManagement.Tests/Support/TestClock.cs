namespace LibraryManagement.Tests.Support;

/// <summary>Controllable clock so tests can move time forward (e.g. to make a loan overdue).</summary>
public sealed class TestClock : TimeProvider
{
    private DateTimeOffset _now;

    public TestClock(DateTimeOffset? start = null)
    {
        _now = start ?? new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);
    }

    public DateTime UtcNow => _now.UtcDateTime;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now = _now.Add(by);
}
