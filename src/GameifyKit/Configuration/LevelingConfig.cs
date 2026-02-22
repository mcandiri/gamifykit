namespace GameifyKit.Configuration;

/// <summary>
/// Defines the XP curve used for leveling.
/// </summary>
public enum LevelCurve
{
    /// <summary>Linear XP requirement per level.</summary>
    Linear,
    /// <summary>Exponential XP requirement per level.</summary>
    Exponential,
    /// <summary>Custom thresholds defined manually.</summary>
    Custom
}

/// <summary>
/// Configuration for the XP leveling system.
/// </summary>
public sealed class LevelingConfig
{
    /// <summary>The leveling curve type.</summary>
    public LevelCurve Curve { get; set; } = LevelCurve.Exponential;

    /// <summary>Base XP required for the first level-up.</summary>
    public int BaseXp { get; set; } = 100;

    /// <summary>Multiplier for exponential curve. Each level needs Multiplier times more XP.</summary>
    public double Multiplier { get; set; } = 1.5;

    /// <summary>Maximum attainable level.</summary>
    public int MaxLevel { get; set; } = 100;

    /// <summary>Custom XP thresholds when using <see cref="LevelCurve.Custom"/>.</summary>
    public int[]? CustomThresholds { get; set; }
}
