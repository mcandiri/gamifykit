using GameifyKit.Events;

namespace GameifyKit.XP;

/// <summary>
/// Event emitted when a player levels up.
/// </summary>
public sealed class LevelUpEvent : GameEvent
{
    /// <summary>The previous level.</summary>
    public int PreviousLevel { get; init; }

    /// <summary>The new level achieved.</summary>
    public int NewLevel { get; init; }

    /// <summary>Total XP at the time of level-up.</summary>
    public long TotalXp { get; init; }
}
