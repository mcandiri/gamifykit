namespace GameifyKit.Analytics;

/// <summary>
/// Distribution of players across tiers.
/// </summary>
public sealed class TierDistribution
{
    /// <summary>Tier name.</summary>
    public string TierName { get; init; } = string.Empty;

    /// <summary>Number of players in this tier.</summary>
    public int PlayerCount { get; init; }

    /// <summary>Percentage of total players.</summary>
    public double Percentage { get; init; }
}
