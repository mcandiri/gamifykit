namespace GameifyKit.Economy;

/// <summary>
/// Defines a purchasable reward in the virtual economy.
/// </summary>
public sealed class RewardDefinition
{
    /// <summary>Unique identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Cost in virtual currency.</summary>
    public int Cost { get; set; }

    /// <summary>Icon emoji or URL.</summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>Maximum purchases per day (0 = unlimited).</summary>
    public int MaxPurchasesPerDay { get; set; }

    /// <summary>Whether this can only be purchased once.</summary>
    public bool OneTimePurchase { get; set; }

    /// <summary>Category for grouping rewards.</summary>
    public string Category { get; set; } = string.Empty;
}
