using GameifyKit.Events;

namespace GameifyKit.Economy;

/// <summary>
/// Event emitted when a player makes a purchase.
/// </summary>
public sealed class PurchaseEvent : GameEvent
{
    /// <summary>The purchased reward.</summary>
    public RewardDefinition Reward { get; init; } = null!;

    /// <summary>Amount spent.</summary>
    public int AmountSpent { get; init; }

    /// <summary>Remaining balance.</summary>
    public long RemainingBalance { get; init; }
}
