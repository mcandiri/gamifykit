namespace GameifyKit.Events;

/// <summary>
/// Base class for all game events emitted by the engine.
/// </summary>
public abstract class GameEvent
{
    /// <summary>
    /// The user ID associated with this event.
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// The timestamp when this event occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
