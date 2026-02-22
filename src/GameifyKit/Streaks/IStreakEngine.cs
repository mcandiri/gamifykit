namespace GameifyKit.Streaks;

/// <summary>
/// Engine for managing player streaks.
/// </summary>
public interface IStreakEngine
{
    /// <summary>
    /// Records an activity for a streak.
    /// </summary>
    Task<StreakInfo> RecordAsync(string userId, string streakId, CancellationToken ct = default);

    /// <summary>
    /// Gets the current streak info for a player.
    /// </summary>
    Task<StreakInfo?> GetAsync(string userId, string streakId, CancellationToken ct = default);

    /// <summary>
    /// Gets all active streaks for a player.
    /// </summary>
    Task<IReadOnlyList<StreakInfo>> GetAllAsync(string userId, CancellationToken ct = default);
}
