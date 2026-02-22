using GameifyKit.Rules;

namespace GameifyKit.Configuration;

/// <summary>
/// Configuration for anti-cheat and rate limiting rules.
/// </summary>
public sealed class RulesConfig
{
    /// <summary>Maximum XP a player can earn per day.</summary>
    public int MaxDailyXp { get; set; } = 5000;

    /// <summary>Maximum actions per hour before throttling.</summary>
    public int MaxActionsPerHour { get; set; } = 200;

    /// <summary>Cooldown rules keyed by action name.</summary>
    public Dictionary<string, TimeSpan> Cooldowns { get; } = new();

    /// <summary>Callback invoked when suspicious activity is detected.</summary>
    public Func<string, SuspiciousActivityEvent, Task>? OnSuspiciousActivity { get; set; }

    /// <summary>Adds a cooldown rule for a specific action.</summary>
    public void Cooldown(string action, TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(action);
        Cooldowns[action] = duration;
    }
}
