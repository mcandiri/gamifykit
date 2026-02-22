namespace GameifyKit.Achievements;

/// <summary>
/// Represents a player's progress toward an achievement.
/// </summary>
public sealed class AchievementProgress
{
    /// <summary>The achievement definition.</summary>
    public AchievementDefinition Achievement { get; init; } = null!;

    /// <summary>Whether the achievement has been unlocked.</summary>
    public bool Unlocked { get; init; }

    /// <summary>When the achievement was unlocked, if applicable.</summary>
    public DateTimeOffset? UnlockedAt { get; init; }

    /// <summary>Current progress value (for counter-based achievements).</summary>
    public long CurrentValue { get; init; }

    /// <summary>Target value (for counter-based achievements).</summary>
    public int TargetValue { get; init; }

    /// <summary>Progress as a ratio (0.0 to 1.0).</summary>
    public double Progress => TargetValue > 0 ? Math.Min(1.0, (double)CurrentValue / TargetValue) : (Unlocked ? 1.0 : 0.0);
}
