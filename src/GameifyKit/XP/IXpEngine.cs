namespace GameifyKit.XP;

/// <summary>
/// Engine for managing player XP and leveling.
/// </summary>
public interface IXpEngine
{
    /// <summary>
    /// Awards XP to a player.
    /// </summary>
    /// <param name="userId">The player's unique identifier.</param>
    /// <param name="amount">The base XP amount to award.</param>
    /// <param name="action">The action that triggered the XP award.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the XP award operation.</returns>
    Task<XpResult> AddAsync(string userId, int amount, string action, CancellationToken ct = default);

    /// <summary>
    /// Gets the total XP for a player.
    /// </summary>
    Task<long> GetTotalXpAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Gets the current level for a player.
    /// </summary>
    Task<int> GetLevelAsync(string userId, CancellationToken ct = default);
}
