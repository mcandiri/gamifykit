namespace GameifyKit.Boosts;

/// <summary>
/// Engine for managing XP boosts.
/// </summary>
public interface IBoostEngine
{
    /// <summary>
    /// Activates a boost for a player.
    /// </summary>
    Task ActivateAsync(string userId, XpBoost boost, CancellationToken ct = default);

    /// <summary>
    /// Gets the current combined XP multiplier for a player.
    /// </summary>
    Task<double> GetMultiplierAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Gets all active boosts for a player.
    /// </summary>
    Task<IReadOnlyList<XpBoost>> GetActiveBoostsAsync(string userId, CancellationToken ct = default);
}
