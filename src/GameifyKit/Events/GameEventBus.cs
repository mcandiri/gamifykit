using Microsoft.Extensions.Logging;

namespace GameifyKit.Events;

/// <summary>
/// In-process event bus for game events.
/// </summary>
public sealed class GameEventBus : IGameEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();
    private readonly object _lock = new();
    private readonly ILogger<GameEventBus> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameEventBus"/> class.
    /// </summary>
    public GameEventBus(ILogger<GameEventBus> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task PublishAsync<TEvent>(TEvent gameEvent, CancellationToken ct = default) where TEvent : GameEvent
    {
        ArgumentNullException.ThrowIfNull(gameEvent);

        List<Delegate> handlers;
        lock (_lock)
        {
            if (!_handlers.TryGetValue(typeof(TEvent), out var list))
                return;
            handlers = new List<Delegate>(list);
        }

        foreach (var handler in handlers)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await ((Func<TEvent, Task>)handler)(gameEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling event {EventType} for user {UserId}",
                    typeof(TEvent).Name, gameEvent.UserId);
            }
        }
    }

    /// <inheritdoc />
    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : GameEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        lock (_lock)
        {
            if (!_handlers.TryGetValue(typeof(TEvent), out var list))
            {
                list = new List<Delegate>();
                _handlers[typeof(TEvent)] = list;
            }
            list.Add(handler);
        }
    }
}
