namespace GameifyKit.Events;

/// <summary>
/// Event bus for publishing and subscribing to game events.
/// </summary>
public interface IGameEventBus
{
    /// <summary>
    /// Publishes an event to all registered handlers.
    /// </summary>
    Task PublishAsync<TEvent>(TEvent gameEvent, CancellationToken ct = default) where TEvent : GameEvent;

    /// <summary>
    /// Subscribes a handler to a specific event type.
    /// </summary>
    void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : GameEvent;
}
