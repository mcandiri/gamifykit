using GameifyKit.Events;
using GameifyKit.Storage;
using Microsoft.Extensions.Logging;

namespace GameifyKit.Streaks;

/// <summary>
/// Default implementation of the streak engine.
/// </summary>
public sealed class StreakEngine : IStreakEngine
{
    private readonly IGameStore _store;
    private readonly IGameEventBus _eventBus;
    private readonly IReadOnlyList<StreakDefinition> _definitions;
    private readonly ILogger<StreakEngine> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreakEngine"/> class.
    /// </summary>
    public StreakEngine(
        IGameStore store,
        IGameEventBus eventBus,
        IReadOnlyList<StreakDefinition> definitions,
        ILogger<StreakEngine> logger)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<StreakInfo> RecordAsync(string userId, string streakId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(streakId);

        var def = _definitions.FirstOrDefault(s => s.Id == streakId)
            ?? throw new ArgumentException($"Streak '{streakId}' not found.", nameof(streakId));

        var data = await _store.GetStreakDataAsync(userId, streakId, ct);
        var now = DateTimeOffset.UtcNow;

        int currentStreak = data.CurrentStreak;
        int bestStreak = data.BestStreak;
        var lastRecorded = data.LastRecordedAt;

        if (lastRecorded.HasValue)
        {
            var periodDuration = def.Period == StreakPeriod.Daily ? TimeSpan.FromDays(1) : TimeSpan.FromDays(7);
            var elapsed = now - lastRecorded.Value;

            if (elapsed < periodDuration)
            {
                // Already recorded this period
                return new StreakInfo
                {
                    Definition = def,
                    CurrentStreak = currentStreak,
                    BestStreak = bestStreak,
                    LastRecordedAt = lastRecorded,
                    IsAlive = true
                };
            }

            if (elapsed <= periodDuration + def.GracePeriod)
            {
                currentStreak++;
            }
            else
            {
                currentStreak = 1; // Streak broken
            }
        }
        else
        {
            currentStreak = 1;
        }

        if (currentStreak > bestStreak)
            bestStreak = currentStreak;

        await _store.SetStreakDataAsync(userId, streakId, currentStreak, bestStreak, now, ct);

        // Check milestones
        string? milestoneReached = null;
        foreach (var milestone in def.Milestones.OrderBy(m => m.Days))
        {
            if (currentStreak == milestone.Days)
            {
                milestoneReached = milestone.Badge;
                await _eventBus.PublishAsync(new StreakMilestoneEvent
                {
                    UserId = userId,
                    Streak = def,
                    Milestone = milestone,
                    CurrentStreak = currentStreak
                }, ct);

                _logger.LogInformation("Player {UserId} reached streak milestone: {Badge}", userId, milestone.Badge);
                break;
            }
        }

        return new StreakInfo
        {
            Definition = def,
            CurrentStreak = currentStreak,
            BestStreak = bestStreak,
            LastRecordedAt = now,
            IsAlive = true,
            MilestoneReached = milestoneReached
        };
    }

    /// <inheritdoc />
    public async Task<StreakInfo?> GetAsync(string userId, string streakId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(streakId);

        var def = _definitions.FirstOrDefault(s => s.Id == streakId);
        if (def == null) return null;

        var data = await _store.GetStreakDataAsync(userId, streakId, ct);
        var now = DateTimeOffset.UtcNow;
        var periodDuration = def.Period == StreakPeriod.Daily ? TimeSpan.FromDays(1) : TimeSpan.FromDays(7);

        bool isAlive = data.LastRecordedAt.HasValue &&
            (now - data.LastRecordedAt.Value) <= periodDuration + def.GracePeriod;

        return new StreakInfo
        {
            Definition = def,
            CurrentStreak = isAlive ? data.CurrentStreak : 0,
            BestStreak = data.BestStreak,
            LastRecordedAt = data.LastRecordedAt,
            IsAlive = isAlive
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StreakInfo>> GetAllAsync(string userId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var result = new List<StreakInfo>();
        foreach (var def in _definitions)
        {
            var info = await GetAsync(userId, def.Id, ct);
            if (info != null) result.Add(info);
        }
        return result;
    }
}

/// <summary>
/// Builder for configuring streaks.
/// </summary>
public sealed class StreakBuilder
{
    private readonly List<StreakDefinition> _streaks;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreakBuilder"/> class.
    /// </summary>
    public StreakBuilder(List<StreakDefinition> streaks)
    {
        _streaks = streaks;
    }

    /// <summary>
    /// Defines a new streak type.
    /// </summary>
    public void Define(string id, Action<StreakDefinition> configure)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(configure);

        var streak = new StreakDefinition { Id = id };
        configure(streak);
        _streaks.Add(streak);
    }
}
