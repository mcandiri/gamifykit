namespace GameifyKit.Quests;

/// <summary>
/// Represents a player's progress on a quest.
/// </summary>
public sealed class QuestProgress
{
    /// <summary>The quest definition.</summary>
    public QuestDefinition Quest { get; init; } = null!;

    /// <summary>IDs of completed steps.</summary>
    public HashSet<string> CompletedSteps { get; init; } = new();

    /// <summary>Total number of steps.</summary>
    public int TotalSteps => Quest.Steps.Length;

    /// <summary>Number of completed steps.</summary>
    public int CompletedCount => CompletedSteps.Count;

    /// <summary>Whether all steps are completed.</summary>
    public bool IsCompleted => CompletedCount >= TotalSteps;

    /// <summary>The next incomplete step, if any.</summary>
    public QuestStep? NextStep => Quest.Steps.FirstOrDefault(s => !CompletedSteps.Contains(s.Id));

    /// <summary>When the quest was started.</summary>
    public DateTimeOffset StartedAt { get; init; }

    /// <summary>Remaining time if the quest has a time limit.</summary>
    public TimeSpan? TimeRemaining
    {
        get
        {
            if (Quest.TimeLimit == null) return null;
            var elapsed = DateTimeOffset.UtcNow - StartedAt;
            var remaining = Quest.TimeLimit.Value - elapsed;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }
}
