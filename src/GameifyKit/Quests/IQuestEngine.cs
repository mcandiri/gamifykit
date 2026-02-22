namespace GameifyKit.Quests;

/// <summary>
/// Engine for managing quests and missions.
/// </summary>
public interface IQuestEngine
{
    /// <summary>
    /// Assigns a quest to a player.
    /// </summary>
    Task AssignAsync(string userId, string questId, CancellationToken ct = default);

    /// <summary>
    /// Progresses a quest step for a player.
    /// </summary>
    Task<QuestProgress> ProgressAsync(string userId, string questId, string stepId, CancellationToken ct = default);

    /// <summary>
    /// Gets all active quests for a player.
    /// </summary>
    Task<IReadOnlyList<QuestProgress>> GetActiveAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Gets progress for a specific quest.
    /// </summary>
    Task<QuestProgress?> GetProgressAsync(string userId, string questId, CancellationToken ct = default);
}
