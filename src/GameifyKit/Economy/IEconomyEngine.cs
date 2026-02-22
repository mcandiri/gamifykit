namespace GameifyKit.Economy;

/// <summary>
/// Engine for managing the virtual economy.
/// </summary>
public interface IEconomyEngine
{
    /// <summary>
    /// Gets a player's wallet information.
    /// </summary>
    Task<WalletInfo> GetWalletAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Purchases a reward for a player.
    /// </summary>
    Task<PurchaseResult> PurchaseAsync(string userId, string rewardId, CancellationToken ct = default);

    /// <summary>
    /// Awards currency to a player (e.g., from XP conversion).
    /// </summary>
    Task AwardCurrencyAsync(string userId, long amount, CancellationToken ct = default);

    /// <summary>
    /// Gets available rewards.
    /// </summary>
    Task<IReadOnlyList<RewardDefinition>> GetRewardsAsync(CancellationToken ct = default);
}
