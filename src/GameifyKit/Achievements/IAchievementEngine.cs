namespace GameifyKit.Achievements;

/// <summary>
/// Engine for managing player achievements.
/// </summary>
public interface IAchievementEngine
{
    /// <summary>
    /// Increments a counter for achievement tracking.
    /// </summary>
    Task IncrementAsync(string userId, string counter, CancellationToken ct = default);

    /// <summary>
    /// Sets a stat value for achievement condition checks.
    /// </summary>
    Task SetStatAsync(string userId, string key, long value, CancellationToken ct = default);

    /// <summary>
    /// Checks and unlocks any newly earned achievements.
    /// </summary>
    /// <returns>List of newly unlocked achievements.</returns>
    Task<IReadOnlyList<AchievementDefinition>> CheckAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Gets all achievement progress for a player.
    /// </summary>
    Task<IReadOnlyList<AchievementProgress>> GetProgressAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Gets all unlocked achievements for a player.
    /// </summary>
    Task<IReadOnlyList<AchievementDefinition>> GetUnlockedAsync(string userId, CancellationToken ct = default);
}
