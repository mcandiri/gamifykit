namespace GameifyKit.Rules;

/// <summary>
/// Engine for anti-cheat and rate limiting rules.
/// </summary>
public interface IRuleEngine
{
    /// <summary>
    /// Validates whether an action is allowed.
    /// </summary>
    Task<RuleValidationResult> ValidateAsync(string userId, string action, int xpAmount, CancellationToken ct = default);

    /// <summary>
    /// Records an action for rate limiting tracking.
    /// </summary>
    Task RecordActionAsync(string userId, string action, int xpAmount, CancellationToken ct = default);
}
