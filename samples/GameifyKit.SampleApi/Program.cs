using GameifyKit.Achievements;
using GameifyKit.Configuration;
using GameifyKit.Extensions;
using GameifyKit.Leaderboard;
using GameifyKit.Quests;
using GameifyKit.SampleApi.Endpoints;
using GameifyKit.Streaks;
using GameifyKit.XP;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddGameifyKit(options =>
{
    // Storage — InMemory for demo
    options.UseInMemoryStore();

    // XP & Leveling
    options.ConfigureLeveling(lvl =>
    {
        lvl.Curve = LevelCurve.Exponential;
        lvl.BaseXp = 100;
        lvl.Multiplier = 1.5;
        lvl.MaxLevel = 100;
    });

    // Achievements
    options.ConfigureAchievements(ach =>
    {
        ach.UseBuiltIn(BuiltInAchievements.FirstLogin);
        ach.UseBuiltIn(BuiltInAchievements.Streak7);
        ach.UseBuiltIn(BuiltInAchievements.Streak30);
        ach.UseBuiltIn(BuiltInAchievements.Level10);
        ach.UseBuiltIn(BuiltInAchievements.Level50);

        ach.Define("quiz-master", a =>
        {
            a.Name = "Quiz Master";
            a.Description = "Complete 50 quizzes";
            a.Icon = "trophy";
            a.Counter = "quizzes_completed";
            a.Target = 50;
            a.XpReward = 500;
            a.Tier = AchievementTier.Gold;
        });
    });

    // Quests
    options.ConfigureQuests(q =>
    {
        q.Define("onboarding", quest =>
        {
            quest.Name = "Getting Started";
            quest.Description = "Complete your first week";
            quest.Icon = "rocket";
            quest.Steps = new[]
            {
                new QuestStep("complete-profile", "Complete your profile", xpReward: 50),
                new QuestStep("first-quiz", "Take your first quiz", xpReward: 100),
                new QuestStep("score-above-80", "Score above 80%", xpReward: 200),
                new QuestStep("join-leaderboard", "Check the leaderboard", xpReward: 50),
            };
            quest.CompletionBonus = 500;
            quest.TimeLimit = TimeSpan.FromDays(7);
            quest.AutoAssign = true;
        });

        q.Define("weekly-challenge", quest =>
        {
            quest.Name = "Weekly Challenge";
            quest.Description = "Complete 10 quizzes this week";
            quest.Recurring = true;
            quest.ResetPeriod = QuestPeriod.Weekly;
            quest.Steps = new[]
            {
                new QuestStep("quiz-1", "Complete 1 quiz", xpReward: 20),
                new QuestStep("quiz-5", "Complete 5 quizzes", xpReward: 100),
                new QuestStep("quiz-10", "Complete 10 quizzes", xpReward: 250),
            };
            quest.CompletionBonus = 400;
        });
    });

    // Streaks
    options.ConfigureStreaks(s =>
    {
        s.Define("daily-login", streak =>
        {
            streak.Name = "Daily Login";
            streak.Period = StreakPeriod.Daily;
            streak.GracePeriod = TimeSpan.FromHours(36);
            streak.Milestones = new[]
            {
                new StreakMilestone(3, xpBonus: 50, badge: "3-Day Streak"),
                new StreakMilestone(7, xpBonus: 150, badge: "Week Warrior"),
                new StreakMilestone(30, xpBonus: 500, badge: "Monthly Legend"),
                new StreakMilestone(100, xpBonus: 2000, badge: "Unstoppable"),
            };
        });
    });

    // Leaderboard
    options.ConfigureLeaderboard(lb =>
    {
        lb.Periods = new[] { LeaderboardPeriod.Daily, LeaderboardPeriod.Weekly, LeaderboardPeriod.Monthly, LeaderboardPeriod.AllTime };
        lb.DefaultPeriod = LeaderboardPeriod.Weekly;
        lb.Tiers = new[]
        {
            new TierDefinition("bronze", "Bronze", icon: "bronze", maxPercentile: 0.50),
            new TierDefinition("silver", "Silver", icon: "silver", maxPercentile: 0.75),
            new TierDefinition("gold", "Gold", icon: "gold", maxPercentile: 0.90),
            new TierDefinition("platinum", "Platinum", icon: "platinum", maxPercentile: 0.99),
            new TierDefinition("diamond", "Diamond", icon: "diamond", maxPercentile: 1.00),
        };
        lb.PromotionBonusXp = 200;
        lb.DemotionProtectionWeeks = 1;
    });

    // Boosts
    options.ConfigureBoosts(b =>
    {
        b.MaxStackableBoosts = 3;
        b.MaxMultiplier = 5.0;
    });

    // Economy
    options.ConfigureEconomy(e =>
    {
        e.CurrencyName = "coins";
        e.CurrencyIcon = "coins";
        e.XpToCurrencyRatio = 10;

        e.DefineReward("extra-time", r =>
        {
            r.Name = "Extra Exam Time";
            r.Description = "+5 minutes on next exam";
            r.Cost = 100;
            r.Icon = "clock";
            r.MaxPurchasesPerDay = 3;
            r.Category = "exam-perks";
        });

        e.DefineReward("hint-unlock", r =>
        {
            r.Name = "Unlock Hint";
            r.Description = "Reveal a hint during exam";
            r.Cost = 50;
            r.Icon = "lightbulb";
            r.Category = "exam-perks";
        });

        e.DefineReward("custom-avatar", r =>
        {
            r.Name = "Custom Avatar Frame";
            r.Description = "Unlock a special avatar frame";
            r.Cost = 500;
            r.Icon = "frame";
            r.OneTimePurchase = true;
            r.Category = "cosmetics";
        });
    });

    // Anti-Cheat Rules
    options.ConfigureRules(rules =>
    {
        rules.MaxDailyXp = 5000;
        rules.Cooldown("quiz-complete", TimeSpan.FromMinutes(2));
        rules.Cooldown("xp-earn", TimeSpan.FromSeconds(10));
        rules.MaxActionsPerHour = 200;
    });

    // Analytics
    options.EnableAnalytics();

    // Events
    options.OnEvent<LevelUpEvent>(e =>
    {
        Console.WriteLine($"[EVENT] {e.UserId} reached level {e.NewLevel}!");
        return Task.CompletedTask;
    });

    options.OnEvent<AchievementUnlockedEvent>(e =>
    {
        Console.WriteLine($"[EVENT] {e.UserId} unlocked: {e.Achievement.Name}!");
        return Task.CompletedTask;
    });

    options.OnEvent<TierChangeEvent>(e =>
    {
        Console.WriteLine($"[EVENT] {e.UserId} promoted to {e.NewTier.Name}!");
        return Task.CompletedTask;
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Map all endpoints
app.MapXpEndpoints();
app.MapAchievementEndpoints();
app.MapQuestEndpoints();
app.MapLeaderboardEndpoints();
app.MapEconomyEndpoints();

app.Run();
