using GameifyKit.Events;
using GameifyKit.Storage;
using Microsoft.Extensions.Logging;

namespace GameifyKit.Achievements;

/// <summary>
/// Default implementation of the achievement engine.
/// </summary>
public sealed class AchievementEngine : IAchievementEngine
{
    private readonly IGameStore _store;
    private readonly IGameEventBus _eventBus;
    private readonly IReadOnlyList<AchievementDefinition> _definitions;
    private readonly ILogger<AchievementEngine> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AchievementEngine"/> class.
    /// </summary>
    public AchievementEngine(
        IGameStore store,
        IGameEventBus eventBus,
        IReadOnlyList<AchievementDefinition> definitions,
        ILogger<AchievementEngine> logger)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task IncrementAsync(string userId, string counter, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(counter);

        await _store.IncrementCounterAsync(userId, counter, ct);
    }

    /// <inheritdoc />
    public async Task SetStatAsync(string userId, string key, long value, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(key);

        await _store.SetStatAsync(userId, key, value, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AchievementDefinition>> CheckAsync(string userId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var unlocked = await _store.GetUnlockedAchievementIdsAsync(userId, ct);
        var counters = await _store.GetCountersAsync(userId, ct);
        var stats = await _store.GetStatsAsync(userId, ct);
        var lastActivity = await _store.GetLastActivityAsync(userId, ct);

        var context = new AchievementContext(stats, counters, lastActivity);
        var newlyUnlocked = new List<AchievementDefinition>();

        foreach (var def in _definitions)
        {
            if (unlocked.Contains(def.Id))
                continue;

            bool earned = false;

            if (def.Counter != null)
            {
                var current = counters.TryGetValue(def.Counter, out var val) ? val : 0;
                earned = current >= def.Target;
            }
            else if (def.Condition != null)
            {
                try
                {
                    earned = def.Condition(context);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error evaluating condition for achievement {AchievementId}", def.Id);
                }
            }

            if (earned)
            {
                await _store.UnlockAchievementAsync(userId, def.Id, ct);
                newlyUnlocked.Add(def);

                await _eventBus.PublishAsync(new AchievementUnlockedEvent
                {
                    UserId = userId,
                    Achievement = def
                }, ct);

                _logger.LogInformation("Player {UserId} unlocked achievement: {AchievementName}", userId, def.Name);
            }
        }

        return newlyUnlocked;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AchievementProgress>> GetProgressAsync(string userId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var unlocked = await _store.GetUnlockedAchievementsAsync(userId, ct);
        var counters = await _store.GetCountersAsync(userId, ct);
        var result = new List<AchievementProgress>();

        foreach (var def in _definitions)
        {
            var unlockedEntry = unlocked.FirstOrDefault(u => u.AchievementId == def.Id);
            var currentValue = def.Counter != null && counters.TryGetValue(def.Counter, out var val) ? val : 0;

            result.Add(new AchievementProgress
            {
                Achievement = def,
                Unlocked = unlockedEntry != null,
                UnlockedAt = unlockedEntry?.UnlockedAt,
                CurrentValue = currentValue,
                TargetValue = def.Target
            });
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AchievementDefinition>> GetUnlockedAsync(string userId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var unlockedIds = await _store.GetUnlockedAchievementIdsAsync(userId, ct);
        return _definitions.Where(d => unlockedIds.Contains(d.Id)).ToList();
    }
}

/// <summary>
/// Builder for configuring achievements.
/// </summary>
public sealed class AchievementBuilder
{
    private readonly List<AchievementDefinition> _achievements;

    /// <summary>
    /// Initializes a new instance of the <see cref="AchievementBuilder"/> class.
    /// </summary>
    public AchievementBuilder(List<AchievementDefinition> achievements)
    {
        _achievements = achievements;
    }

    /// <summary>
    /// Adds a built-in achievement.
    /// </summary>
    public void UseBuiltIn(AchievementDefinition achievement)
    {
        ArgumentNullException.ThrowIfNull(achievement);
        _achievements.Add(achievement);
    }

    /// <summary>
    /// Defines a custom achievement.
    /// </summary>
    public void Define(string id, Action<AchievementDefinition> configure)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(configure);

        var achievement = new AchievementDefinition { Id = id };
        configure(achievement);
        _achievements.Add(achievement);
    }
}
