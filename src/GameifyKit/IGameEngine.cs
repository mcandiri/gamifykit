using GameifyKit.Achievements;
using GameifyKit.Analytics;
using GameifyKit.Boosts;
using GameifyKit.Economy;
using GameifyKit.Leaderboard;
using GameifyKit.Quests;
using GameifyKit.Rules;
using GameifyKit.Streaks;
using GameifyKit.XP;

namespace GameifyKit;

/// <summary>
/// Represents a player's complete profile.
/// </summary>
public sealed class PlayerProfile
{
    /// <summary>User identifier.</summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>Total accumulated XP.</summary>
    public long TotalXp { get; init; }

    /// <summary>Current level.</summary>
    public int Level { get; init; }

    /// <summary>Progress toward next level (0.0 to 1.0).</summary>
    public double CurrentLevelProgress { get; init; }

    /// <summary>Current tier name.</summary>
    public string? Tier { get; init; }

    /// <summary>Current rank in default leaderboard.</summary>
    public int Rank { get; init; }

    /// <summary>Unlocked achievements.</summary>
    public IReadOnlyList<AchievementDefinition> Achievements { get; init; } = [];

    /// <summary>Active quests.</summary>
    public IReadOnlyList<QuestProgress> ActiveQuests { get; init; } = [];

    /// <summary>Active streaks.</summary>
    public IReadOnlyList<StreakInfo> ActiveStreaks { get; init; } = [];

    /// <summary>Active boosts.</summary>
    public IReadOnlyList<XpBoost> ActiveBoosts { get; init; } = [];

    /// <summary>Wallet information.</summary>
    public WalletInfo? Wallet { get; init; }

    /// <summary>Player stats.</summary>
    public Dictionary<string, long> Stats { get; init; } = new();
}

/// <summary>
/// Main GameifyKit engine interface providing access to all subsystems.
/// </summary>
public interface IGameEngine
{
    /// <summary>XP and leveling engine.</summary>
    IXpEngine Xp { get; }

    /// <summary>Achievement engine.</summary>
    IAchievementEngine Achievements { get; }

    /// <summary>Quest engine.</summary>
    IQuestEngine Quests { get; }

    /// <summary>Streak engine.</summary>
    IStreakEngine Streaks { get; }

    /// <summary>Leaderboard engine.</summary>
    ILeaderboardEngine Leaderboard { get; }

    /// <summary>Boost engine.</summary>
    IBoostEngine Boosts { get; }

    /// <summary>Economy engine.</summary>
    IEconomyEngine Economy { get; }

    /// <summary>Analytics engine.</summary>
    IAnalyticsEngine Analytics { get; }

    /// <summary>Rule engine.</summary>
    IRuleEngine Rules { get; }

    /// <summary>
    /// Gets a complete player profile.
    /// </summary>
    Task<PlayerProfile> GetProfileAsync(string userId, CancellationToken ct = default);
}
