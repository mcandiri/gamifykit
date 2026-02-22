using GameifyKit.Events;

namespace GameifyKit.Quests;

/// <summary>
/// Event emitted when a player completes a quest.
/// </summary>
public sealed class QuestCompletedEvent : GameEvent
{
    /// <summary>The completed quest.</summary>
    public QuestDefinition Quest { get; init; } = null!;

    /// <summary>Total XP earned from the quest (steps + bonus).</summary>
    public int TotalXpEarned { get; init; }
}
