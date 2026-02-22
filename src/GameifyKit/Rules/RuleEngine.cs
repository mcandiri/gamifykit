using System.Collections.Concurrent;
using GameifyKit.Configuration;
using GameifyKit.Events;
using Microsoft.Extensions.Logging;

namespace GameifyKit.Rules;

/// <summary>
/// Default implementation of the rule engine.
/// </summary>
public sealed class RuleEngine : IRuleEngine
{
    private readonly RulesConfig _config;
    private readonly IGameEventBus _eventBus;
    private readonly ILogger<RuleEngine> _logger;

    // In-memory tracking (per process)
    private readonly ConcurrentDictionary<string, DailyXpTracker> _dailyXp = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, DateTimeOffset>> _cooldowns = new();
    private readonly ConcurrentDictionary<string, HourlyActionTracker> _hourlyActions = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RuleEngine"/> class.
    /// </summary>
    public RuleEngine(RulesConfig config, IGameEventBus eventBus, ILogger<RuleEngine> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<RuleValidationResult> ValidateAsync(string userId, string action, int xpAmount, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(action);

        // Check daily XP limit
        var tracker = _dailyXp.GetOrAdd(userId, _ => new DailyXpTracker());
        tracker.ResetIfNewDay();

        if (tracker.TodayXp + xpAmount > _config.MaxDailyXp)
        {
            var violation = new SuspiciousActivityEvent
            {
                UserId = userId,
                Type = "daily_xp_limit_exceeded",
                Details = $"User earned {tracker.TodayXp + xpAmount} XP today (limit: {_config.MaxDailyXp})"
            };

            await _eventBus.PublishAsync(violation, ct);

            if (_config.OnSuspiciousActivity != null)
            {
                await _config.OnSuspiciousActivity(userId, violation);
            }

            return RuleValidationResult.Deny($"Daily XP limit ({_config.MaxDailyXp}) would be exceeded.");
        }

        // Check cooldown
        if (_config.Cooldowns.TryGetValue(action, out var cooldownDuration))
        {
            var userCooldowns = _cooldowns.GetOrAdd(userId, _ => new ConcurrentDictionary<string, DateTimeOffset>());
            if (userCooldowns.TryGetValue(action, out var lastAction))
            {
                if (DateTimeOffset.UtcNow - lastAction < cooldownDuration)
                {
                    return RuleValidationResult.Deny($"Cooldown for '{action}' has not expired.");
                }
            }
        }

        // Check hourly action limit
        var hourlyTracker = _hourlyActions.GetOrAdd(userId, _ => new HourlyActionTracker());
        hourlyTracker.ResetIfNewHour();

        if (hourlyTracker.Count >= _config.MaxActionsPerHour)
        {
            var violation = new SuspiciousActivityEvent
            {
                UserId = userId,
                Type = "hourly_action_limit_exceeded",
                Details = $"User performed {hourlyTracker.Count} actions this hour (limit: {_config.MaxActionsPerHour})"
            };

            await _eventBus.PublishAsync(violation, ct);
            return RuleValidationResult.Deny($"Hourly action limit ({_config.MaxActionsPerHour}) reached.");
        }

        return RuleValidationResult.Allow();
    }

    /// <inheritdoc />
    public Task RecordActionAsync(string userId, string action, int xpAmount, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(action);

        // Update daily XP
        var tracker = _dailyXp.GetOrAdd(userId, _ => new DailyXpTracker());
        tracker.ResetIfNewDay();
        tracker.TodayXp += xpAmount;

        // Update cooldown
        var userCooldowns = _cooldowns.GetOrAdd(userId, _ => new ConcurrentDictionary<string, DateTimeOffset>());
        userCooldowns[action] = DateTimeOffset.UtcNow;

        // Update hourly actions
        var hourlyTracker = _hourlyActions.GetOrAdd(userId, _ => new HourlyActionTracker());
        hourlyTracker.ResetIfNewHour();
        hourlyTracker.Count++;

        return Task.CompletedTask;
    }

    private sealed class DailyXpTracker
    {
        public int TodayXp;
        public DateOnly LastDay = DateOnly.FromDateTime(DateTime.UtcNow);

        public void ResetIfNewDay()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (today != LastDay)
            {
                TodayXp = 0;
                LastDay = today;
            }
        }
    }

    private sealed class HourlyActionTracker
    {
        public int Count;
        public int LastHour = DateTime.UtcNow.Hour;
        public DateOnly LastDay = DateOnly.FromDateTime(DateTime.UtcNow);

        public void ResetIfNewHour()
        {
            var now = DateTime.UtcNow;
            var today = DateOnly.FromDateTime(now);
            if (now.Hour != LastHour || today != LastDay)
            {
                Count = 0;
                LastHour = now.Hour;
                LastDay = today;
            }
        }
    }
}
