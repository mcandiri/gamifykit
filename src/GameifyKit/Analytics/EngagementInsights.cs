namespace GameifyKit.Analytics;

/// <summary>
/// Engagement analytics insights.
/// </summary>
public sealed class EngagementInsights
{
    /// <summary>Total registered players.</summary>
    public int TotalPlayers { get; init; }

    /// <summary>Daily active users.</summary>
    public int DailyActiveUsers { get; init; }

    /// <summary>Weekly active users.</summary>
    public int WeeklyActiveUsers { get; init; }

    /// <summary>Average XP per active player.</summary>
    public double AverageXpPerPlayer { get; init; }

    /// <summary>Average player level.</summary>
    public double AverageLevel { get; init; }

    /// <summary>Achievement unlock rate.</summary>
    public double AchievementUnlockRate { get; init; }

    /// <summary>Active streak percentage.</summary>
    public double ActiveStreakPercentage { get; init; }
}
