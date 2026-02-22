using GameifyKit.Achievements;
using GameifyKit.Analytics;
using GameifyKit.Boosts;
using GameifyKit.Configuration;
using GameifyKit.Economy;
using GameifyKit.Leaderboard;
using GameifyKit.Quests;
using GameifyKit.Rules;
using GameifyKit.Storage;
using GameifyKit.Streaks;
using GameifyKit.XP;

namespace GameifyKit;

/// <summary>
/// Main GameifyKit engine that orchestrates all subsystems.
/// </summary>
public sealed class GameEngine : IGameEngine
{
    private readonly IGameStore _store;
    private readonly LevelCalculator _levelCalculator;
    private readonly LeaderboardConfig _leaderboardConfig;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameEngine"/> class.
    /// </summary>
    public GameEngine(
        IXpEngine xp,
        IAchievementEngine achievements,
        IQuestEngine quests,
        IStreakEngine streaks,
        ILeaderboardEngine leaderboard,
        IBoostEngine boosts,
        IEconomyEngine economy,
        IAnalyticsEngine analytics,
        IRuleEngine rules,
        IGameStore store,
        LevelingConfig levelingConfig,
        LeaderboardConfig leaderboardConfig)
    {
        Xp = xp ?? throw new ArgumentNullException(nameof(xp));
        Achievements = achievements ?? throw new ArgumentNullException(nameof(achievements));
        Quests = quests ?? throw new ArgumentNullException(nameof(quests));
        Streaks = streaks ?? throw new ArgumentNullException(nameof(streaks));
        Leaderboard = leaderboard ?? throw new ArgumentNullException(nameof(leaderboard));
        Boosts = boosts ?? throw new ArgumentNullException(nameof(boosts));
        Economy = economy ?? throw new ArgumentNullException(nameof(economy));
        Analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
        Rules = rules ?? throw new ArgumentNullException(nameof(rules));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _levelCalculator = new LevelCalculator(levelingConfig);
        _leaderboardConfig = leaderboardConfig ?? throw new ArgumentNullException(nameof(leaderboardConfig));
    }

    /// <inheritdoc />
    public IXpEngine Xp { get; }

    /// <inheritdoc />
    public IAchievementEngine Achievements { get; }

    /// <inheritdoc />
    public IQuestEngine Quests { get; }

    /// <inheritdoc />
    public IStreakEngine Streaks { get; }

    /// <inheritdoc />
    public ILeaderboardEngine Leaderboard { get; }

    /// <inheritdoc />
    public IBoostEngine Boosts { get; }

    /// <inheritdoc />
    public IEconomyEngine Economy { get; }

    /// <inheritdoc />
    public IAnalyticsEngine Analytics { get; }

    /// <inheritdoc />
    public IRuleEngine Rules { get; }

    /// <inheritdoc />
    public async Task<PlayerProfile> GetProfileAsync(string userId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var totalXp = await _store.GetPlayerXpAsync(userId, ct);
        var level = _levelCalculator.GetLevel(totalXp);
        var progress = _levelCalculator.GetProgress(totalXp);

        var achievements = await Achievements.GetUnlockedAsync(userId, ct);
        var activeQuests = await Quests.GetActiveAsync(userId, ct);
        var activeStreaks = await Streaks.GetAllAsync(userId, ct);
        var activeBoosts = await Boosts.GetActiveBoostsAsync(userId, ct);
        var wallet = await Economy.GetWalletAsync(userId, ct);
        var standing = await Leaderboard.GetStandingAsync(userId, _leaderboardConfig.DefaultPeriod, ct);
        var stats = await _store.GetStatsAsync(userId, ct);

        return new PlayerProfile
        {
            UserId = userId,
            TotalXp = totalXp,
            Level = level,
            CurrentLevelProgress = progress,
            Tier = standing.Tier,
            Rank = standing.Rank,
            Achievements = achievements,
            ActiveQuests = activeQuests,
            ActiveStreaks = activeStreaks,
            ActiveBoosts = activeBoosts,
            Wallet = wallet,
            Stats = stats
        };
    }
}
