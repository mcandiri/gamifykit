using System.Collections.Concurrent;
using GameifyKit.Boosts;
using GameifyKit.Leaderboard;

namespace GameifyKit.Storage;

/// <summary>
/// In-memory implementation of IGameStore using ConcurrentDictionary.
/// Suitable for testing and prototyping.
/// </summary>
public sealed class InMemoryGameStore : IGameStore
{
    private readonly ConcurrentDictionary<string, long> _playerXp = new();
    private readonly ConcurrentDictionary<string, int> _playerLevels = new();
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastActivity = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, long>> _counters = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, long>> _stats = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, DateTimeOffset>> _achievements = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, HashSet<string>>> _questSteps = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, DateTimeOffset>> _questStartTimes = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _activeQuests = new();
    private readonly ConcurrentDictionary<string, StreakDataRecord> _streaks = new();
    private readonly ConcurrentDictionary<(LeaderboardPeriod, string), LeaderboardEntryData> _leaderboard = new();
    private readonly ConcurrentDictionary<string, List<XpBoost>> _boosts = new();
    private readonly ConcurrentDictionary<string, WalletData> _wallets = new();
    private readonly ConcurrentDictionary<string, List<(string RewardId, DateTimeOffset PurchasedAt)>> _purchases = new();

    // === Player ===

    /// <inheritdoc />
    public Task<long> GetPlayerXpAsync(string userId, CancellationToken ct = default)
        => Task.FromResult(_playerXp.GetOrAdd(userId, 0));

    /// <inheritdoc />
    public Task SetPlayerXpAsync(string userId, long xp, CancellationToken ct = default)
    {
        _playerXp[userId] = xp;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<int> GetPlayerLevelAsync(string userId, CancellationToken ct = default)
        => Task.FromResult(_playerLevels.GetOrAdd(userId, 1));

    /// <inheritdoc />
    public Task SetPlayerLevelAsync(string userId, int level, CancellationToken ct = default)
    {
        _playerLevels[userId] = level;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RecordActivityAsync(string userId, CancellationToken ct = default)
    {
        _lastActivity[userId] = DateTimeOffset.UtcNow;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<DateTimeOffset> GetLastActivityAsync(string userId, CancellationToken ct = default)
        => Task.FromResult(_lastActivity.GetOrAdd(userId, DateTimeOffset.UtcNow));

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetAllPlayerIdsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<string>>(_playerXp.Keys.ToList());

    // === Counters & Stats ===

    /// <inheritdoc />
    public Task IncrementCounterAsync(string userId, string counter, CancellationToken ct = default)
    {
        var userCounters = _counters.GetOrAdd(userId, _ => new ConcurrentDictionary<string, long>());
        userCounters.AddOrUpdate(counter, 1, (_, v) => v + 1);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<Dictionary<string, long>> GetCountersAsync(string userId, CancellationToken ct = default)
    {
        var userCounters = _counters.GetOrAdd(userId, _ => new ConcurrentDictionary<string, long>());
        return Task.FromResult(new Dictionary<string, long>(userCounters));
    }

    /// <inheritdoc />
    public Task SetStatAsync(string userId, string key, long value, CancellationToken ct = default)
    {
        var userStats = _stats.GetOrAdd(userId, _ => new ConcurrentDictionary<string, long>());
        userStats[key] = value;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<Dictionary<string, long>> GetStatsAsync(string userId, CancellationToken ct = default)
    {
        var userStats = _stats.GetOrAdd(userId, _ => new ConcurrentDictionary<string, long>());
        return Task.FromResult(new Dictionary<string, long>(userStats));
    }

    // === Achievements ===

    /// <inheritdoc />
    public Task UnlockAchievementAsync(string userId, string achievementId, CancellationToken ct = default)
    {
        var userAch = _achievements.GetOrAdd(userId, _ => new ConcurrentDictionary<string, DateTimeOffset>());
        userAch.TryAdd(achievementId, DateTimeOffset.UtcNow);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<HashSet<string>> GetUnlockedAchievementIdsAsync(string userId, CancellationToken ct = default)
    {
        var userAch = _achievements.GetOrAdd(userId, _ => new ConcurrentDictionary<string, DateTimeOffset>());
        return Task.FromResult(new HashSet<string>(userAch.Keys));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<UnlockedAchievementRecord>> GetUnlockedAchievementsAsync(string userId, CancellationToken ct = default)
    {
        var userAch = _achievements.GetOrAdd(userId, _ => new ConcurrentDictionary<string, DateTimeOffset>());
        var records = userAch.Select(kv => new UnlockedAchievementRecord
        {
            AchievementId = kv.Key,
            UnlockedAt = kv.Value
        }).ToList();
        return Task.FromResult<IReadOnlyList<UnlockedAchievementRecord>>(records);
    }

    // === Quests ===

    /// <inheritdoc />
    public Task AssignQuestAsync(string userId, string questId, CancellationToken ct = default)
    {
        var quests = _activeQuests.GetOrAdd(userId, _ => new HashSet<string>());
        lock (quests)
        {
            quests.Add(questId);
        }
        var startTimes = _questStartTimes.GetOrAdd(userId, _ => new ConcurrentDictionary<string, DateTimeOffset>());
        startTimes.TryAdd(questId, DateTimeOffset.UtcNow);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CompleteQuestStepAsync(string userId, string questId, string stepId, CancellationToken ct = default)
    {
        var userSteps = _questSteps.GetOrAdd(userId, _ => new ConcurrentDictionary<string, HashSet<string>>());
        var questCompleted = userSteps.GetOrAdd(questId, _ => new HashSet<string>());
        lock (questCompleted)
        {
            questCompleted.Add(stepId);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetCompletedQuestStepsAsync(string userId, string questId, CancellationToken ct = default)
    {
        var userSteps = _questSteps.GetOrAdd(userId, _ => new ConcurrentDictionary<string, HashSet<string>>());
        var steps = userSteps.GetOrAdd(questId, _ => new HashSet<string>());
        List<string> result;
        lock (steps)
        {
            result = steps.ToList();
        }
        return Task.FromResult<IReadOnlyList<string>>(result);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetActiveQuestIdsAsync(string userId, CancellationToken ct = default)
    {
        var quests = _activeQuests.GetOrAdd(userId, _ => new HashSet<string>());
        List<string> result;
        lock (quests)
        {
            result = quests.ToList();
        }
        return Task.FromResult<IReadOnlyList<string>>(result);
    }

    /// <inheritdoc />
    public Task<DateTimeOffset> GetQuestStartTimeAsync(string userId, string questId, CancellationToken ct = default)
    {
        var startTimes = _questStartTimes.GetOrAdd(userId, _ => new ConcurrentDictionary<string, DateTimeOffset>());
        return Task.FromResult(startTimes.GetOrAdd(questId, DateTimeOffset.UtcNow));
    }

    // === Streaks ===

    /// <inheritdoc />
    public Task<StreakDataRecord> GetStreakDataAsync(string userId, string streakId, CancellationToken ct = default)
    {
        var key = $"{userId}:{streakId}";
        var data = _streaks.GetOrAdd(key, _ => new StreakDataRecord());
        return Task.FromResult(data);
    }

    /// <inheritdoc />
    public Task SetStreakDataAsync(string userId, string streakId, int current, int best, DateTimeOffset lastRecorded, CancellationToken ct = default)
    {
        var key = $"{userId}:{streakId}";
        _streaks[key] = new StreakDataRecord
        {
            CurrentStreak = current,
            BestStreak = best,
            LastRecordedAt = lastRecorded
        };
        return Task.CompletedTask;
    }

    // === Leaderboard ===

    /// <inheritdoc />
    public Task<IReadOnlyList<LeaderboardEntryData>> GetLeaderboardEntriesAsync(LeaderboardPeriod period, int count, CancellationToken ct = default)
    {
        var entries = _leaderboard
            .Where(kv => kv.Key.Item1 == period)
            .Select(kv => kv.Value)
            .OrderByDescending(e => e.Xp)
            .Take(count)
            .ToList();
        return Task.FromResult<IReadOnlyList<LeaderboardEntryData>>(entries);
    }

    /// <inheritdoc />
    public Task UpdateLeaderboardAsync(string userId, LeaderboardPeriod period, long xp, CancellationToken ct = default)
    {
        var key = (period, userId);
        var level = _playerLevels.GetOrAdd(userId, 1);
        _leaderboard[key] = new LeaderboardEntryData
        {
            UserId = userId,
            Xp = xp,
            Level = level
        };
        return Task.CompletedTask;
    }

    // === Boosts ===

    /// <inheritdoc />
    public Task AddBoostAsync(string userId, XpBoost boost, CancellationToken ct = default)
    {
        var userBoosts = _boosts.GetOrAdd(userId, _ => new List<XpBoost>());
        lock (userBoosts)
        {
            userBoosts.Add(boost);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<XpBoost>> GetActiveBoostsAsync(string userId, CancellationToken ct = default)
    {
        if (!_boosts.TryGetValue(userId, out var userBoosts))
            return Task.FromResult<IReadOnlyList<XpBoost>>(Array.Empty<XpBoost>());

        List<XpBoost> active;
        lock (userBoosts)
        {
            active = userBoosts.Where(b => b.IsActive).ToList();
        }
        return Task.FromResult<IReadOnlyList<XpBoost>>(active);
    }

    // === Economy ===

    /// <inheritdoc />
    public Task<WalletData> GetWalletAsync(string userId, CancellationToken ct = default)
        => Task.FromResult(_wallets.GetOrAdd(userId, _ => new WalletData()));

    /// <inheritdoc />
    public Task SetWalletBalanceAsync(string userId, long balance, CancellationToken ct = default)
    {
        _wallets.AddOrUpdate(userId,
            _ => new WalletData { Balance = balance },
            (_, existing) => new WalletData { Balance = balance, LifetimeEarned = existing.LifetimeEarned });
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task AddLifetimeEarnedAsync(string userId, long amount, CancellationToken ct = default)
    {
        _wallets.AddOrUpdate(userId,
            _ => new WalletData { LifetimeEarned = amount },
            (_, existing) => new WalletData { Balance = existing.Balance, LifetimeEarned = existing.LifetimeEarned + amount });
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RecordPurchaseAsync(string userId, string rewardId, CancellationToken ct = default)
    {
        var userPurchases = _purchases.GetOrAdd(userId, _ => new List<(string, DateTimeOffset)>());
        lock (userPurchases)
        {
            userPurchases.Add((rewardId, DateTimeOffset.UtcNow));
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> HasPurchasedAsync(string userId, string rewardId, CancellationToken ct = default)
    {
        if (!_purchases.TryGetValue(userId, out var userPurchases))
            return Task.FromResult(false);

        bool result;
        lock (userPurchases)
        {
            result = userPurchases.Any(p => p.RewardId == rewardId);
        }
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<int> GetDailyPurchaseCountAsync(string userId, string rewardId, CancellationToken ct = default)
    {
        if (!_purchases.TryGetValue(userId, out var userPurchases))
            return Task.FromResult(0);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        int count;
        lock (userPurchases)
        {
            count = userPurchases.Count(p =>
                p.RewardId == rewardId &&
                DateOnly.FromDateTime(p.PurchasedAt.UtcDateTime) == today);
        }
        return Task.FromResult(count);
    }
}
