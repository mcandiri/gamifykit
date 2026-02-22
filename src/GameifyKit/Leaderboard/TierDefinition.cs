namespace GameifyKit.Leaderboard;

/// <summary>
/// Defines a leaderboard tier.
/// </summary>
public sealed class TierDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TierDefinition"/> class.
    /// </summary>
    public TierDefinition(string id, string name, string icon = "", double maxPercentile = 1.0)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Icon = icon;
        MaxPercentile = maxPercentile;
    }

    /// <summary>Unique tier identifier.</summary>
    public string Id { get; }

    /// <summary>Display name.</summary>
    public string Name { get; }

    /// <summary>Tier icon.</summary>
    public string Icon { get; }

    /// <summary>Maximum percentile for this tier (0.0 to 1.0).</summary>
    public double MaxPercentile { get; }
}
