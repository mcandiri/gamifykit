using GameifyKit.Leaderboard;

namespace GameifyKit.Configuration;

/// <summary>
/// Configuration for the leaderboard system.
/// </summary>
public sealed class LeaderboardConfig
{
    /// <summary>Active leaderboard periods.</summary>
    public LeaderboardPeriod[] Periods { get; set; } = [LeaderboardPeriod.Weekly];

    /// <summary>Default period when none is specified.</summary>
    public LeaderboardPeriod DefaultPeriod { get; set; } = LeaderboardPeriod.Weekly;

    /// <summary>Tier definitions from lowest to highest.</summary>
    public TierDefinition[] Tiers { get; set; } = [];

    /// <summary>Bonus XP awarded on tier promotion.</summary>
    public int PromotionBonusXp { get; set; } = 200;

    /// <summary>Number of weeks of demotion protection after promotion.</summary>
    public int DemotionProtectionWeeks { get; set; } = 1;
}
