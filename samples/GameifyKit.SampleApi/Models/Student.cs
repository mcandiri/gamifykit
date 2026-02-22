namespace GameifyKit.SampleApi.Models;

/// <summary>
/// Represents a student in the sample application.
/// </summary>
public sealed class Student
{
    /// <summary>Unique student identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Student's display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Student's email.</summary>
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Request to add XP to a student.
/// </summary>
public sealed class AddXpRequest
{
    /// <summary>Amount of XP to award.</summary>
    public int Amount { get; set; }

    /// <summary>Action that triggered the XP award.</summary>
    public string Action { get; set; } = "general";
}

/// <summary>
/// Request to progress a quest step.
/// </summary>
public sealed class QuestProgressRequest
{
    /// <summary>Quest identifier.</summary>
    public string QuestId { get; set; } = string.Empty;

    /// <summary>Step identifier.</summary>
    public string StepId { get; set; } = string.Empty;
}

/// <summary>
/// Request to activate a boost.
/// </summary>
public sealed class ActivateBoostRequest
{
    /// <summary>XP multiplier.</summary>
    public double Multiplier { get; set; } = 2.0;

    /// <summary>Duration in hours.</summary>
    public int DurationHours { get; set; } = 24;

    /// <summary>Reason for the boost.</summary>
    public string Reason { get; set; } = "manual";
}

/// <summary>
/// Request to purchase a reward.
/// </summary>
public sealed class PurchaseRequest
{
    /// <summary>Reward identifier.</summary>
    public string RewardId { get; set; } = string.Empty;
}
