using GameifyKit.Configuration;
using GameifyKit.Events;
using GameifyKit.Storage;
using Microsoft.Extensions.Logging;

namespace GameifyKit.Boosts;

/// <summary>
/// Default implementation of the boost engine.
/// </summary>
public sealed class BoostEngine : IBoostEngine
{
    private readonly IGameStore _store;
    private readonly IGameEventBus _eventBus;
    private readonly BoostsConfig _config;
    private readonly ILogger<BoostEngine> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BoostEngine"/> class.
    /// </summary>
    public BoostEngine(
        IGameStore store,
        IGameEventBus eventBus,
        BoostsConfig config,
        ILogger<BoostEngine> logger)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task ActivateAsync(string userId, XpBoost boost, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(boost);

        var activeBoosts = await _store.GetActiveBoostsAsync(userId, ct);
        if (activeBoosts.Count >= _config.MaxStackableBoosts)
        {
            _logger.LogWarning("Player {UserId} already has {Count} active boosts (max: {Max})",
                userId, activeBoosts.Count, _config.MaxStackableBoosts);
            throw new InvalidOperationException(
                $"Maximum stackable boosts ({_config.MaxStackableBoosts}) reached.");
        }

        boost.ActivatedAt = DateTimeOffset.UtcNow;
        await _store.AddBoostAsync(userId, boost, ct);

        await _eventBus.PublishAsync(new BoostActivatedEvent
        {
            UserId = userId,
            Boost = boost
        }, ct);

        _logger.LogInformation("Activated {Multiplier}x boost for {UserId} ({Reason})",
            boost.Multiplier, userId, boost.Reason);
    }

    /// <inheritdoc />
    public async Task<double> GetMultiplierAsync(string userId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var activeBoosts = await _store.GetActiveBoostsAsync(userId, ct);
        if (activeBoosts.Count == 0)
            return 1.0;

        double total = 1.0;
        foreach (var boost in activeBoosts)
        {
            total *= boost.Multiplier;
        }

        return Math.Min(total, _config.MaxMultiplier);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<XpBoost>> GetActiveBoostsAsync(string userId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        return await _store.GetActiveBoostsAsync(userId, ct);
    }
}
