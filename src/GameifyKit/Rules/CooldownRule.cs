namespace GameifyKit.Rules;

/// <summary>
/// Tracks cooldown state for a specific action.
/// </summary>
public sealed class CooldownState
{
    /// <summary>Last time the action was performed.</summary>
    public DateTimeOffset LastActionTime { get; set; }

    /// <summary>Required cooldown duration.</summary>
    public TimeSpan CooldownDuration { get; set; }

    /// <summary>Whether the cooldown period has elapsed.</summary>
    public bool IsReady => DateTimeOffset.UtcNow - LastActionTime >= CooldownDuration;

    /// <summary>Time remaining until cooldown expires.</summary>
    public TimeSpan TimeRemaining
    {
        get
        {
            var remaining = CooldownDuration - (DateTimeOffset.UtcNow - LastActionTime);
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }
}
