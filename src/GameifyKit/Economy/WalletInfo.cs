namespace GameifyKit.Economy;

/// <summary>
/// A player's wallet balance information.
/// </summary>
public sealed class WalletInfo
{
    /// <summary>Current balance.</summary>
    public long Balance { get; init; }

    /// <summary>Display name of the currency.</summary>
    public string CurrencyName { get; init; } = "coins";

    /// <summary>Total currency earned over lifetime.</summary>
    public long Lifetime { get; init; }
}
