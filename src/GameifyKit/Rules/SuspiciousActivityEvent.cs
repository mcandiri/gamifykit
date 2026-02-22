using GameifyKit.Events;

namespace GameifyKit.Rules;

/// <summary>
/// Event emitted when suspicious activity is detected.
/// </summary>
public sealed class SuspiciousActivityEvent : GameEvent
{
    /// <summary>Type of violation detected.</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>Human-readable details of the violation.</summary>
    public string Details { get; init; } = string.Empty;

    /// <summary>The violation description (alias for Details).</summary>
    public string Violation => Details;
}
