using GameifyKit.Events;

namespace GameifyKit.Leaderboard;

/// <summary>
/// Direction of tier change.
/// </summary>
public enum TierDirection
{
    /// <summary>Promoted to a higher tier.</summary>
    Promoted,
    /// <summary>Demoted to a lower tier.</summary>
    Demoted
}

/// <summary>
/// Event emitted when a player's tier changes.
/// </summary>
public sealed class TierChangeEvent : GameEvent
{
    /// <summary>Previous tier.</summary>
    public TierDefinition? PreviousTier { get; init; }

    /// <summary>New tier.</summary>
    public TierDefinition NewTier { get; init; } = null!;

    /// <summary>Direction of the change.</summary>
    public TierDirection Direction { get; init; }
}
