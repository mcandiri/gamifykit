namespace GameifyKit.XP;

/// <summary>
/// Result of an XP award operation.
/// </summary>
public sealed class XpResult
{
    /// <summary>Base XP before multipliers.</summary>
    public int BaseXp { get; init; }

    /// <summary>Active XP multiplier.</summary>
    public double Multiplier { get; init; } = 1.0;

    /// <summary>Final XP after multipliers.</summary>
    public int FinalXp { get; init; }

    /// <summary>Total accumulated XP for the player.</summary>
    public long TotalXp { get; init; }

    /// <summary>Current level after this XP award.</summary>
    public int Level { get; init; }

    /// <summary>Whether the player leveled up from this award.</summary>
    public bool LeveledUp { get; init; }

    /// <summary>Whether the XP was throttled by rules.</summary>
    public bool Throttled { get; init; }
}
