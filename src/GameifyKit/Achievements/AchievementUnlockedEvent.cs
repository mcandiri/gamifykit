using GameifyKit.Events;

namespace GameifyKit.Achievements;

/// <summary>
/// Event emitted when a player unlocks an achievement.
/// </summary>
public sealed class AchievementUnlockedEvent : GameEvent
{
    /// <summary>The achievement that was unlocked.</summary>
    public AchievementDefinition Achievement { get; init; } = null!;
}
