using GameifyKit.Boosts;
using GameifyKit.Leaderboard;

namespace GameifyKit.Storage;

/// <summary>
/// Unlocked achievement record.
/// </summary>
public sealed class UnlockedAchievementRecord
{
    /// <summary>Achievement identifier.</summary>
    public string AchievementId { get; init; } = string.Empty;

    /// <summary>When the achievement was unlocked.</summary>
    public DateTimeOffset UnlockedAt { get; init; }
}

/// <summary>
/// Streak data record.
/// </summary>
public sealed class StreakDataRecord
{
    /// <summary>Current streak count.</summary>
    public int CurrentStreak { get; init; }

    /// <summary>Best streak ever.</summary>
    public int BestStreak { get; init; }

    /// <summary>Last recorded time.</summary>
    public DateTimeOffset? LastRecordedAt { get; init; }
}

/// <summary>
/// Leaderboard entry data from storage.
/// </summary>
public sealed class LeaderboardEntryData
{
    /// <summary>User identifier.</summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>Display name.</summary>
    public string? DisplayName { get; init; }

    /// <summary>XP in this period.</summary>
    public long Xp { get; init; }

    /// <summary>Current level.</summary>
    public int Level { get; init; }
}

/// <summary>
/// Wallet data record.
/// </summary>
public sealed class WalletData
{
    /// <summary>Current balance.</summary>
    public long Balance { get; init; }

    /// <summary>Lifetime earnings.</summary>
    public long LifetimeEarned { get; init; }
}

/// <summary>
/// Storage abstraction for all game data.
/// </summary>
public interface IGameStore
{
    // === Player ===

    /// <summary>Gets a player's total XP.</summary>
    Task<long> GetPlayerXpAsync(string userId, CancellationToken ct = default);

    /// <summary>Sets a player's total XP.</summary>
    Task SetPlayerXpAsync(string userId, long xp, CancellationToken ct = default);

    /// <summary>Gets a player's current level.</summary>
    Task<int> GetPlayerLevelAsync(string userId, CancellationToken ct = default);

    /// <summary>Sets a player's level.</summary>
    Task SetPlayerLevelAsync(string userId, int level, CancellationToken ct = default);

    /// <summary>Records player activity (updates last activity time).</summary>
    Task RecordActivityAsync(string userId, CancellationToken ct = default);

    /// <summary>Gets the last activity time for a player.</summary>
    Task<DateTimeOffset> GetLastActivityAsync(string userId, CancellationToken ct = default);

    /// <summary>Gets all player IDs.</summary>
    Task<IReadOnlyList<string>> GetAllPlayerIdsAsync(CancellationToken ct = default);

    // === Counters & Stats ===

    /// <summary>Increments a counter.</summary>
    Task IncrementCounterAsync(string userId, string counter, CancellationToken ct = default);

    /// <summary>Gets all counters for a player.</summary>
    Task<Dictionary<string, long>> GetCountersAsync(string userId, CancellationToken ct = default);

    /// <summary>Sets a stat value.</summary>
    Task SetStatAsync(string userId, string key, long value, CancellationToken ct = default);

    /// <summary>Gets all stats for a player.</summary>
    Task<Dictionary<string, long>> GetStatsAsync(string userId, CancellationToken ct = default);

    // === Achievements ===

    /// <summary>Unlocks an achievement.</summary>
    Task UnlockAchievementAsync(string userId, string achievementId, CancellationToken ct = default);

    /// <summary>Gets IDs of unlocked achievements.</summary>
    Task<HashSet<string>> GetUnlockedAchievementIdsAsync(string userId, CancellationToken ct = default);

    /// <summary>Gets unlocked achievement records.</summary>
    Task<IReadOnlyList<UnlockedAchievementRecord>> GetUnlockedAchievementsAsync(string userId, CancellationToken ct = default);

    // === Quests ===

    /// <summary>Assigns a quest to a player.</summary>
    Task AssignQuestAsync(string userId, string questId, CancellationToken ct = default);

    /// <summary>Completes a quest step.</summary>
    Task CompleteQuestStepAsync(string userId, string questId, string stepId, CancellationToken ct = default);

    /// <summary>Gets completed step IDs for a quest.</summary>
    Task<IReadOnlyList<string>> GetCompletedQuestStepsAsync(string userId, string questId, CancellationToken ct = default);

    /// <summary>Gets active quest IDs for a player.</summary>
    Task<IReadOnlyList<string>> GetActiveQuestIdsAsync(string userId, CancellationToken ct = default);

    /// <summary>Gets when a quest was started.</summary>
    Task<DateTimeOffset> GetQuestStartTimeAsync(string userId, string questId, CancellationToken ct = default);

    // === Streaks ===

    /// <summary>Gets streak data.</summary>
    Task<StreakDataRecord> GetStreakDataAsync(string userId, string streakId, CancellationToken ct = default);

    /// <summary>Sets streak data.</summary>
    Task SetStreakDataAsync(string userId, string streakId, int current, int best, DateTimeOffset lastRecorded, CancellationToken ct = default);

    // === Leaderboard ===

    /// <summary>Gets leaderboard entries.</summary>
    Task<IReadOnlyList<LeaderboardEntryData>> GetLeaderboardEntriesAsync(LeaderboardPeriod period, int count, CancellationToken ct = default);

    /// <summary>Updates a player's leaderboard entry.</summary>
    Task UpdateLeaderboardAsync(string userId, LeaderboardPeriod period, long xp, CancellationToken ct = default);

    // === Boosts ===

    /// <summary>Adds a boost.</summary>
    Task AddBoostAsync(string userId, XpBoost boost, CancellationToken ct = default);

    /// <summary>Gets active (non-expired) boosts.</summary>
    Task<IReadOnlyList<XpBoost>> GetActiveBoostsAsync(string userId, CancellationToken ct = default);

    // === Economy ===

    /// <summary>Gets wallet data.</summary>
    Task<WalletData> GetWalletAsync(string userId, CancellationToken ct = default);

    /// <summary>Sets wallet balance.</summary>
    Task SetWalletBalanceAsync(string userId, long balance, CancellationToken ct = default);

    /// <summary>Adds to lifetime earned.</summary>
    Task AddLifetimeEarnedAsync(string userId, long amount, CancellationToken ct = default);

    /// <summary>Records a purchase.</summary>
    Task RecordPurchaseAsync(string userId, string rewardId, CancellationToken ct = default);

    /// <summary>Whether a player has purchased a specific reward.</summary>
    Task<bool> HasPurchasedAsync(string userId, string rewardId, CancellationToken ct = default);

    /// <summary>Gets daily purchase count for a reward.</summary>
    Task<int> GetDailyPurchaseCountAsync(string userId, string rewardId, CancellationToken ct = default);
}
