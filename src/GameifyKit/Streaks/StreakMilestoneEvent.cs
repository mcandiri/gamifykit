using GameifyKit.Events;

namespace GameifyKit.Streaks;

/// <summary>
/// Event emitted when a player reaches a streak milestone.
/// </summary>
public sealed class StreakMilestoneEvent : GameEvent
{
    /// <summary>The streak definition.</summary>
    public StreakDefinition Streak { get; init; } = null!;

    /// <summary>The milestone reached.</summary>
    public StreakMilestone Milestone { get; init; } = null!;

    /// <summary>Current streak count.</summary>
    public int CurrentStreak { get; init; }
}
