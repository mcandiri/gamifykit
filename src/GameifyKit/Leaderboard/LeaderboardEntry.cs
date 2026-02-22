namespace GameifyKit.Leaderboard;

/// <summary>
/// A single entry in the leaderboard.
/// </summary>
public sealed class LeaderboardEntry
{
    /// <summary>Rank position (1-based).</summary>
    public int Rank { get; init; }

    /// <summary>Player identifier.</summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>Player display name.</summary>
    public string? DisplayName { get; init; }

    /// <summary>XP earned in this period.</summary>
    public long Xp { get; init; }

    /// <summary>Current player level.</summary>
    public int Level { get; init; }

    /// <summary>Current tier name.</summary>
    public string? Tier { get; init; }
}
