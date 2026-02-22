namespace GameifyKit.Analytics;

/// <summary>
/// Analytics for quest completion.
/// </summary>
public sealed class QuestAnalytics
{
    /// <summary>Quest identifier.</summary>
    public string QuestId { get; init; } = string.Empty;

    /// <summary>Quest name.</summary>
    public string QuestName { get; init; } = string.Empty;

    /// <summary>Number of players who started the quest.</summary>
    public int StartedCount { get; init; }

    /// <summary>Number of players who completed the quest.</summary>
    public int CompletedCount { get; init; }

    /// <summary>Completion rate.</summary>
    public double CompletionRate => StartedCount > 0 ? (double)CompletedCount / StartedCount : 0;
}
