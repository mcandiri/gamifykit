namespace GameifyKit.Streaks;

/// <summary>
/// Period type for streaks.
/// </summary>
public enum StreakPeriod
{
    /// <summary>Daily streak.</summary>
    Daily,
    /// <summary>Weekly streak.</summary>
    Weekly
}

/// <summary>
/// Milestone within a streak with rewards.
/// </summary>
public sealed class StreakMilestone
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StreakMilestone"/> class.
    /// </summary>
    public StreakMilestone(int days, int xpBonus = 0, string? badge = null)
    {
        Days = days;
        XpBonus = xpBonus;
        Badge = badge;
    }

    /// <summary>Number of streak days/weeks to reach this milestone.</summary>
    public int Days { get; }

    /// <summary>XP bonus for reaching this milestone.</summary>
    public int XpBonus { get; }

    /// <summary>Badge text for this milestone.</summary>
    public string? Badge { get; }
}

/// <summary>
/// Defines a streak type.
/// </summary>
public sealed class StreakDefinition
{
    /// <summary>Unique identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Streak period type.</summary>
    public StreakPeriod Period { get; set; } = StreakPeriod.Daily;

    /// <summary>Grace period before the streak breaks.</summary>
    public TimeSpan GracePeriod { get; set; } = TimeSpan.FromHours(36);

    /// <summary>Milestones with rewards.</summary>
    public StreakMilestone[] Milestones { get; set; } = [];

    /// <summary>Minimum activities per period to count.</summary>
    public int MinActivityCount { get; set; } = 1;
}

/// <summary>
/// Current streak state for a player.
/// </summary>
public sealed class StreakInfo
{
    /// <summary>The streak definition.</summary>
    public StreakDefinition Definition { get; init; } = null!;

    /// <summary>Current streak count.</summary>
    public int CurrentStreak { get; init; }

    /// <summary>Best streak ever achieved.</summary>
    public int BestStreak { get; init; }

    /// <summary>Last recorded activity time.</summary>
    public DateTimeOffset? LastRecordedAt { get; init; }

    /// <summary>Whether the streak is still alive (within grace period).</summary>
    public bool IsAlive { get; init; }

    /// <summary>Milestone badge reached, if any.</summary>
    public string? MilestoneReached { get; init; }
}
