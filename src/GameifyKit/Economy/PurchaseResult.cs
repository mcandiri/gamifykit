namespace GameifyKit.Economy;

/// <summary>
/// Result of a purchase attempt.
/// </summary>
public sealed class PurchaseResult
{
    /// <summary>Whether the purchase was successful.</summary>
    public bool Success { get; init; }

    /// <summary>Reason for failure, if applicable.</summary>
    public string? Reason { get; init; }

    /// <summary>Remaining balance after purchase.</summary>
    public long RemainingBalance { get; init; }

    /// <summary>The purchased reward, if successful.</summary>
    public RewardDefinition? Reward { get; init; }
}
