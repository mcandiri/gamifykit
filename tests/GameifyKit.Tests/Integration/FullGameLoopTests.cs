using FluentAssertions;
using GameifyKit.Achievements;
using Xunit;
using GameifyKit.Boosts;
using GameifyKit.Configuration;
using GameifyKit.Economy;
using GameifyKit.Events;
using GameifyKit.Leaderboard;
using GameifyKit.Quests;
using GameifyKit.Rules;
using GameifyKit.Storage;
using GameifyKit.Streaks;
using GameifyKit.XP;
using GameifyKit.Analytics;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameifyKit.Tests.Integration;

public class FullGameLoopTests
{
    private readonly InMemoryGameStore _store;
    private readonly GameEventBus _eventBus;
    private readonly RuleEngine _ruleEngine;
    private readonly BoostEngine _boostEngine;
    private readonly XpEngine _xpEngine;
    private readonly AchievementEngine _achievementEngine;
    private readonly QuestEngine _questEngine;
    private readonly StreakEngine _streakEngine;
    private readonly LeaderboardEngine _leaderboardEngine;
    private readonly EconomyEngine _economyEngine;
    private readonly AnalyticsEngine _analyticsEngine;

    private readonly LevelingConfig _levelingConfig;
    private readonly LeaderboardConfig _leaderboardConfig;

    public FullGameLoopTests()
    {
        _store = new InMemoryGameStore();
        _eventBus = new GameEventBus(NullLogger<GameEventBus>.Instance);

        var rulesConfig = new RulesConfig
        {
            MaxDailyXp = 50000,
            MaxActionsPerHour = 1000
        };
        _ruleEngine = new RuleEngine(rulesConfig, _eventBus, NullLogger<RuleEngine>.Instance);

        var boostsConfig = new BoostsConfig { MaxStackableBoosts = 3, MaxMultiplier = 5.0 };
        _boostEngine = new BoostEngine(_store, _eventBus, boostsConfig, NullLogger<BoostEngine>.Instance);

        _levelingConfig = new LevelingConfig
        {
            Curve = LevelCurve.Linear,
            BaseXp = 100,
            MaxLevel = 100
        };
        _xpEngine = new XpEngine(_store, _eventBus, _boostEngine, _ruleEngine, _levelingConfig, NullLogger<XpEngine>.Instance);

        var achievements = new List<AchievementDefinition>
        {
            new AchievementDefinition
            {
                Id = "first-quiz",
                Name = "First Quiz",
                Counter = "quizzes_completed",
                Target = 1,
                XpReward = 50,
                Tier = AchievementTier.Bronze
            },
            new AchievementDefinition
            {
                Id = "level-5",
                Name = "Level 5",
                Counter = "level",
                Target = 5,
                XpReward = 200,
                Tier = AchievementTier.Silver
            }
        };
        _achievementEngine = new AchievementEngine(_store, _eventBus, achievements, NullLogger<AchievementEngine>.Instance);

        var quests = new List<QuestDefinition>
        {
            new QuestDefinition
            {
                Id = "onboarding",
                Name = "Onboarding Quest",
                Steps = new[]
                {
                    new QuestStep("complete-profile", "Complete your profile", 25),
                    new QuestStep("first-quiz", "Complete your first quiz", 50),
                    new QuestStep("review-results", "Review your results", 25)
                },
                CompletionBonus = 100
            }
        };
        _questEngine = new QuestEngine(_store, _eventBus, quests, NullLogger<QuestEngine>.Instance);

        var streaks = new List<StreakDefinition>
        {
            new StreakDefinition
            {
                Id = "daily-activity",
                Name = "Daily Activity",
                Period = StreakPeriod.Daily,
                GracePeriod = TimeSpan.FromHours(36),
                Milestones = new[]
                {
                    new StreakMilestone(3, 50, "3-day streak"),
                    new StreakMilestone(7, 150, "Week warrior")
                }
            }
        };
        _streakEngine = new StreakEngine(_store, _eventBus, streaks, NullLogger<StreakEngine>.Instance);

        _leaderboardConfig = new LeaderboardConfig
        {
            Periods = new[] { LeaderboardPeriod.Weekly },
            DefaultPeriod = LeaderboardPeriod.Weekly,
            Tiers = new[]
            {
                new TierDefinition("bronze", "Bronze", "B", 0.5),
                new TierDefinition("silver", "Silver", "S", 0.8),
                new TierDefinition("gold", "Gold", "G", 1.0)
            }
        };
        _leaderboardEngine = new LeaderboardEngine(_store, _eventBus, _leaderboardConfig, NullLogger<LeaderboardEngine>.Instance);

        var economyConfig = new EconomyConfig { CurrencyName = "coins", XpToCurrencyRatio = 10 };
        economyConfig.DefineReward("badge", r =>
        {
            r.Name = "Custom Badge";
            r.Cost = 100;
        });
        _economyEngine = new EconomyEngine(_store, _eventBus, economyConfig, NullLogger<EconomyEngine>.Instance);

        _analyticsEngine = new AnalyticsEngine(_store, NullLogger<AnalyticsEngine>.Instance);
    }

    [Fact]
    public async Task FullGameLoop_PlayerJourney()
    {
        const string userId = "player1";

        // --- Step 1: Player joins, earn first XP ---
        var xpResult = await _xpEngine.AddAsync(userId, 50, "quiz");
        xpResult.TotalXp.Should().Be(50);
        xpResult.Level.Should().Be(1);
        xpResult.LeveledUp.Should().BeFalse();

        // --- Step 2: Player earns enough XP to level up ---
        xpResult = await _xpEngine.AddAsync(userId, 60, "quiz");
        xpResult.TotalXp.Should().Be(110);
        xpResult.Level.Should().Be(2);
        xpResult.LeveledUp.Should().BeTrue();

        // --- Step 3: Track achievement progress ---
        await _achievementEngine.IncrementAsync(userId, "quizzes_completed");
        var unlocked = await _achievementEngine.CheckAsync(userId);
        unlocked.Should().HaveCount(1);
        unlocked[0].Id.Should().Be("first-quiz");

        // Verify it won't unlock again
        await _achievementEngine.IncrementAsync(userId, "quizzes_completed");
        var secondCheck = await _achievementEngine.CheckAsync(userId);
        secondCheck.Should().BeEmpty();

        // --- Step 4: Start and progress a quest ---
        await _questEngine.AssignAsync(userId, "onboarding");
        var questProgress = await _questEngine.ProgressAsync(userId, "onboarding", "complete-profile");
        questProgress.CompletedCount.Should().Be(1);
        questProgress.IsCompleted.Should().BeFalse();

        questProgress = await _questEngine.ProgressAsync(userId, "onboarding", "first-quiz");
        questProgress.CompletedCount.Should().Be(2);

        questProgress = await _questEngine.ProgressAsync(userId, "onboarding", "review-results");
        questProgress.IsCompleted.Should().BeTrue();

        // --- Step 5: Record daily streak ---
        var streakInfo = await _streakEngine.RecordAsync(userId, "daily-activity");
        streakInfo.CurrentStreak.Should().Be(1);
        streakInfo.IsAlive.Should().BeTrue();

        // --- Step 6: Update leaderboard ---
        var totalXp = await _xpEngine.GetTotalXpAsync(userId);
        await _leaderboardEngine.UpdateAsync(userId, totalXp);

        var standing = await _leaderboardEngine.GetStandingAsync(userId, LeaderboardPeriod.Weekly);
        standing.Rank.Should().Be(1); // Only player
        standing.Xp.Should().Be(totalXp);

        // --- Step 7: Award currency and make a purchase ---
        await _economyEngine.AwardCurrencyAsync(userId, 500);
        var wallet = await _economyEngine.GetWalletAsync(userId);
        wallet.Balance.Should().Be(500);

        var purchaseResult = await _economyEngine.PurchaseAsync(userId, "badge");
        purchaseResult.Success.Should().BeTrue();
        purchaseResult.RemainingBalance.Should().Be(400);

        // --- Step 8: Check analytics ---
        var insights = await _analyticsEngine.GetInsightsAsync();
        insights.TotalPlayers.Should().BeGreaterThanOrEqualTo(1);
        insights.AverageXpPerPlayer.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task FullGameLoop_MultiplePlayersOnLeaderboard()
    {
        // Add 5 players with different XP
        for (int i = 1; i <= 5; i++)
        {
            var userId = $"player{i}";
            await _xpEngine.AddAsync(userId, i * 200, "quiz");
            var xp = await _xpEngine.GetTotalXpAsync(userId);
            await _leaderboardEngine.UpdateAsync(userId, xp);
        }

        // Check leaderboard
        var top = await _leaderboardEngine.GetTopAsync(LeaderboardPeriod.Weekly, 10);
        top.Should().HaveCount(5);
        top[0].UserId.Should().Be("player5"); // Highest XP
        top[0].Rank.Should().Be(1);

        // Check standing for middle player
        var standing = await _leaderboardEngine.GetStandingAsync("player3", LeaderboardPeriod.Weekly);
        standing.Rank.Should().Be(3);
    }

    [Fact]
    public async Task FullGameLoop_XpWithBoosts()
    {
        const string userId = "boosted-player";

        // Activate a 2x boost
        await _boostEngine.ActivateAsync(userId, new XpBoost
        {
            Multiplier = 2.0,
            Duration = TimeSpan.FromHours(1),
            Reason = "double-xp-event"
        });

        // Earn XP with boost active
        var result = await _xpEngine.AddAsync(userId, 50, "quiz");

        result.BaseXp.Should().Be(50);
        result.Multiplier.Should().Be(2.0);
        result.FinalXp.Should().Be(100);
        result.TotalXp.Should().Be(100);
        result.Level.Should().Be(2); // 100 XP with linear 100/level => level 2
    }

    [Fact]
    public async Task FullGameLoop_AchievementWithCondition()
    {
        const string userId = "achiever";

        var achievements = new List<AchievementDefinition>
        {
            new AchievementDefinition
            {
                Id = "high-scorer",
                Name = "High Scorer",
                Condition = ctx => ctx.GetStat("total_score") >= 500
            }
        };
        var engine = new AchievementEngine(_store, _eventBus, achievements, NullLogger<AchievementEngine>.Instance);

        // Set stat below threshold
        await engine.SetStatAsync(userId, "total_score", 300);
        var check1 = await engine.CheckAsync(userId);
        check1.Should().BeEmpty();

        // Set stat above threshold
        await engine.SetStatAsync(userId, "total_score", 500);
        var check2 = await engine.CheckAsync(userId);
        check2.Should().HaveCount(1);
        check2[0].Id.Should().Be("high-scorer");
    }

    [Fact]
    public async Task FullGameLoop_EconomyWithPurchaseLimits()
    {
        const string userId = "shopper";

        var config = new EconomyConfig { CurrencyName = "gems" };
        config.DefineReward("potion", r =>
        {
            r.Name = "Potion";
            r.Cost = 10;
            r.MaxPurchasesPerDay = 2;
        });
        config.DefineReward("legendary-sword", r =>
        {
            r.Name = "Legendary Sword";
            r.Cost = 500;
            r.OneTimePurchase = true;
        });
        var economy = new EconomyEngine(_store, _eventBus, config, NullLogger<EconomyEngine>.Instance);

        await economy.AwardCurrencyAsync(userId, 10000);

        // Buy potions up to daily limit
        var p1 = await economy.PurchaseAsync(userId, "potion");
        var p2 = await economy.PurchaseAsync(userId, "potion");
        var p3 = await economy.PurchaseAsync(userId, "potion");

        p1.Success.Should().BeTrue();
        p2.Success.Should().BeTrue();
        p3.Success.Should().BeFalse();

        // Buy legendary sword once
        var s1 = await economy.PurchaseAsync(userId, "legendary-sword");
        var s2 = await economy.PurchaseAsync(userId, "legendary-sword");

        s1.Success.Should().BeTrue();
        s2.Success.Should().BeFalse();
        s2.Reason.Should().Contain("one-time");
    }

    [Fact]
    public async Task FullGameLoop_GameEngineProfile()
    {
        const string userId = "profile-player";

        var gameEngine = new GameEngine(
            _xpEngine, _achievementEngine, _questEngine, _streakEngine,
            _leaderboardEngine, _boostEngine, _economyEngine, _analyticsEngine,
            _ruleEngine, _store, _levelingConfig, _leaderboardConfig);

        // Setup some data
        await _xpEngine.AddAsync(userId, 250, "quiz");
        await _achievementEngine.IncrementAsync(userId, "quizzes_completed");
        await _achievementEngine.CheckAsync(userId);
        await _questEngine.AssignAsync(userId, "onboarding");
        await _streakEngine.RecordAsync(userId, "daily-activity");
        await _economyEngine.AwardCurrencyAsync(userId, 300);

        // Get profile
        var profile = await gameEngine.GetProfileAsync(userId);

        profile.UserId.Should().Be(userId);
        profile.TotalXp.Should().Be(250);
        profile.Level.Should().Be(3); // 250 XP / 100 per level = level 3
        profile.Achievements.Should().HaveCount(1);
        profile.ActiveQuests.Should().HaveCount(1);
        profile.ActiveStreaks.Should().HaveCount(1);
        profile.Wallet.Should().NotBeNull();
        profile.Wallet!.Balance.Should().Be(300);
    }
}
