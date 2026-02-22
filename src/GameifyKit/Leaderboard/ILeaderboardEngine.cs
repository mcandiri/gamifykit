namespace GameifyKit.Leaderboard;

/// <summary>
/// Engine for managing leaderboards and tier rankings.
/// </summary>
public interface ILeaderboardEngine
{
    /// <summary>
    /// Gets the top entries for a leaderboard period.
    /// </summary>
    Task<IReadOnlyList<LeaderboardEntry>> GetTopAsync(LeaderboardPeriod period, int count = 10, CancellationToken ct = default);

    /// <summary>
    /// Gets a player's standing in a leaderboard period.
    /// </summary>
    Task<PlayerStanding> GetStandingAsync(string userId, LeaderboardPeriod period, CancellationToken ct = default);

    /// <summary>
    /// Updates a player's XP in the leaderboard.
    /// </summary>
    Task UpdateAsync(string userId, long xp, CancellationToken ct = default);
}
