namespace GameifyKit.Quests;

/// <summary>
/// A single step within a quest.
/// </summary>
public sealed class QuestStep
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QuestStep"/> class.
    /// </summary>
    public QuestStep(string id, string description, int xpReward = 0)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        XpReward = xpReward;
    }

    /// <summary>Unique step identifier within the quest.</summary>
    public string Id { get; }

    /// <summary>Description of what the player needs to do.</summary>
    public string Description { get; }

    /// <summary>XP reward for completing this step.</summary>
    public int XpReward { get; }
}
