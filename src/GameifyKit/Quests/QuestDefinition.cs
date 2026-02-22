namespace GameifyKit.Quests;

/// <summary>
/// Period for recurring quests.
/// </summary>
public enum QuestPeriod
{
    /// <summary>Resets daily.</summary>
    Daily,
    /// <summary>Resets weekly.</summary>
    Weekly,
    /// <summary>Resets monthly.</summary>
    Monthly
}

/// <summary>
/// Defines a quest that players can complete.
/// </summary>
public sealed class QuestDefinition
{
    /// <summary>Unique identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Description of the quest.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Icon emoji or URL.</summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>Steps to complete the quest.</summary>
    public QuestStep[] Steps { get; set; } = [];

    /// <summary>Bonus XP for completing all steps.</summary>
    public int CompletionBonus { get; set; }

    /// <summary>Optional time limit to complete the quest.</summary>
    public TimeSpan? TimeLimit { get; set; }

    /// <summary>Whether this quest auto-assigns to new users.</summary>
    public bool AutoAssign { get; set; }

    /// <summary>Whether this quest recurs.</summary>
    public bool Recurring { get; set; }

    /// <summary>Reset period for recurring quests.</summary>
    public QuestPeriod? ResetPeriod { get; set; }
}
