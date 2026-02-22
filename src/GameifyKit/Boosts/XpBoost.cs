namespace GameifyKit.Boosts;

/// <summary>
/// Represents an XP multiplier boost.
/// </summary>
public sealed class XpBoost
{
    /// <summary>Unique boost identifier (auto-generated if not set).</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>XP multiplier (e.g., 2.0 = double XP).</summary>
    public double Multiplier { get; set; } = 1.0;

    /// <summary>Duration of the boost.</summary>
    public TimeSpan Duration { get; set; }

    /// <summary>Reason or source of the boost.</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>When the boost was activated.</summary>
    public DateTimeOffset ActivatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>When the boost expires.</summary>
    public DateTimeOffset ExpiresAt => ActivatedAt + Duration;

    /// <summary>Whether the boost is still active.</summary>
    public bool IsActive => DateTimeOffset.UtcNow < ExpiresAt;
}
