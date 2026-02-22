namespace GameifyKit.Leaderboard;

/// <summary>
/// A player's standing in the leaderboard.
/// </summary>
public sealed class PlayerStanding
{
    /// <summary>Current rank position.</summary>
    public int Rank { get; init; }

    /// <summary>Current tier name.</summary>
    public string? Tier { get; init; }

    /// <summary>Current tier icon.</summary>
    public string? TierIcon { get; init; }

    /// <summary>XP earned in this period.</summary>
    public long Xp { get; init; }

    /// <summary>Points needed to reach the next tier.</summary>
    public long PointsToNextTier { get; init; }

    /// <summary>Whether the player is at risk of demotion.</summary>
    public bool AtRiskOfDemotion { get; init; }
}
