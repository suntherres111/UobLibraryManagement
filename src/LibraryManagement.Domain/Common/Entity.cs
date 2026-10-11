namespace LibraryManagement.Domain.Common;

/// <summary>
/// Base class for all persisted domain entities.
/// Provides identity and audit timestamps (set centrally by the DbContext, not by callers).
/// </summary>
public abstract class Entity
{
    public int Id { get; protected set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public void MarkCreated(DateTime utcNow) => CreatedAtUtc = utcNow;

    public void MarkUpdated(DateTime utcNow) => UpdatedAtUtc = utcNow;
}
