namespace GameifyKit.Achievements;

/// <summary>
/// Tier classification for achievements.
/// </summary>
public enum AchievementTier
{
    /// <summary>Bronze tier.</summary>
    Bronze,
    /// <summary>Silver tier.</summary>
    Silver,
    /// <summary>Gold tier.</summary>
    Gold,
    /// <summary>Platinum tier.</summary>
    Platinum
}

/// <summary>
/// Context passed to achievement condition checks.
/// </summary>
public sealed class AchievementContext
{
    private readonly Dictionary<string, long> _stats;
    private readonly Dictionary<string, long> _counters;

    /// <summary>
    /// Initializes a new instance of the <see cref="AchievementContext"/> class.
    /// </summary>
    public AchievementContext(Dictionary<string, long> stats, Dictionary<string, long> counters, DateTimeOffset lastActivityTime)
    {
        _stats = stats;
        _counters = counters;
        LastActivityTime = lastActivityTime;
    }

    /// <summary>Last activity time for the player.</summary>
    public DateTimeOffset LastActivityTime { get; }

    /// <summary>Gets a stat value for the player.</summary>
    public long GetStat(string key) => _stats.TryGetValue(key, out var val) ? val : 0;

    /// <summary>Gets a counter value for the player.</summary>
    public long GetCounter(string key) => _counters.TryGetValue(key, out var val) ? val : 0;
}

/// <summary>
/// Defines an achievement that players can unlock.
/// </summary>
public sealed class AchievementDefinition
{
    /// <summary>Unique identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Description of how to unlock.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Icon emoji or URL.</summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>Counter key to track (e.g., "quizzes_completed").</summary>
    public string? Counter { get; set; }

    /// <summary>Target value for counter-based achievements.</summary>
    public int Target { get; set; }

    /// <summary>Custom condition for achievement unlock.</summary>
    public Func<AchievementContext, bool>? Condition { get; set; }

    /// <summary>XP reward for unlocking.</summary>
    public int XpReward { get; set; }

    /// <summary>Achievement tier classification.</summary>
    public AchievementTier Tier { get; set; } = AchievementTier.Bronze;

    /// <summary>Whether this achievement is hidden until unlocked.</summary>
    public bool Secret { get; set; }
}
