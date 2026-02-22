using GameifyKit.Events;

namespace GameifyKit.Boosts;

/// <summary>
/// Event emitted when an XP boost is activated.
/// </summary>
public sealed class BoostActivatedEvent : GameEvent
{
    /// <summary>The activated boost.</summary>
    public XpBoost Boost { get; init; } = null!;
}
