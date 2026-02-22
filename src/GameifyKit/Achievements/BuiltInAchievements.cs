namespace GameifyKit.Achievements;

/// <summary>
/// Pre-defined common achievements.
/// </summary>
public static class BuiltInAchievements
{
    /// <summary>First login achievement.</summary>
    public static readonly AchievementDefinition FirstLogin = new()
    {
        Id = "first-login",
        Name = "First Steps",
        Description = "Log in for the first time",
        Icon = "👋",
        Counter = "logins",
        Target = 1,
        XpReward = 50,
        Tier = AchievementTier.Bronze
    };

    /// <summary>7-day streak achievement.</summary>
    public static readonly AchievementDefinition Streak7 = new()
    {
        Id = "streak-7",
        Name = "Week Warrior",
        Description = "Maintain a 7-day streak",
        Icon = "🔥",
        Counter = "best_streak",
        Target = 7,
        XpReward = 150,
        Tier = AchievementTier.Silver
    };

    /// <summary>30-day streak achievement.</summary>
    public static readonly AchievementDefinition Streak30 = new()
    {
        Id = "streak-30",
        Name = "Monthly Legend",
        Description = "Maintain a 30-day streak",
        Icon = "🌟",
        Counter = "best_streak",
        Target = 30,
        XpReward = 500,
        Tier = AchievementTier.Gold
    };

    /// <summary>Reach level 10 achievement.</summary>
    public static readonly AchievementDefinition Level10 = new()
    {
        Id = "level-10",
        Name = "Rising Star",
        Description = "Reach level 10",
        Icon = "⭐",
        Counter = "level",
        Target = 10,
        XpReward = 200,
        Tier = AchievementTier.Silver
    };

    /// <summary>Reach level 50 achievement.</summary>
    public static readonly AchievementDefinition Level50 = new()
    {
        Id = "level-50",
        Name = "Grandmaster",
        Description = "Reach level 50",
        Icon = "👑",
        Counter = "level",
        Target = 50,
        XpReward = 1000,
        Tier = AchievementTier.Platinum
    };
}
