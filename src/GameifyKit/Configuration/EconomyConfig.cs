using GameifyKit.Economy;

namespace GameifyKit.Configuration;

/// <summary>
/// Configuration for the virtual economy system.
/// </summary>
public sealed class EconomyConfig
{
    /// <summary>Display name for the currency.</summary>
    public string CurrencyName { get; set; } = "coins";

    /// <summary>Icon for the currency.</summary>
    public string CurrencyIcon { get; set; } = "coins";

    /// <summary>How many XP equals 1 unit of currency.</summary>
    public int XpToCurrencyRatio { get; set; } = 10;

    /// <summary>Reward definitions available for purchase.</summary>
    public List<RewardDefinition> Rewards { get; } = new();

    /// <summary>Defines a purchasable reward.</summary>
    public void DefineReward(string id, Action<RewardDefinition> configure)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(configure);

        var reward = new RewardDefinition { Id = id };
        configure(reward);
        Rewards.Add(reward);
    }
}
