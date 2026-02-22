namespace GameifyKit.Rules;

/// <summary>
/// Result of a rule validation.
/// </summary>
public sealed class RuleValidationResult
{
    /// <summary>Whether the action is allowed.</summary>
    public bool Allowed { get; init; }

    /// <summary>Reason for denial, if applicable.</summary>
    public string? Reason { get; init; }

    /// <summary>Creates an allowed result.</summary>
    public static RuleValidationResult Allow() => new() { Allowed = true };

    /// <summary>Creates a denied result.</summary>
    public static RuleValidationResult Deny(string reason) => new() { Allowed = false, Reason = reason };
}
