using System.Data;
using Dapper;
using GameifyKit.Boosts;
using GameifyKit.Leaderboard;
using Npgsql;

namespace GameifyKit.Storage;

/// <summary>
/// PostgreSQL implementation of <see cref="IGameStore"/> using Dapper and Npgsql.
/// Tables are auto-created on first use with the <c>GameifyKit_</c> prefix.
/// This class is thread-safe; each call opens its own connection from the pool.
/// </summary>
public sealed class PostgreSqlGameStore : IGameStore
{
    private readonly string _connectionString;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private volatile bool _initialized;

    /// <summary>
    /// Initializes a new instance of <see cref="PostgreSqlGameStore"/>.
    /// </summary>
    /// <param name="connectionString">PostgreSQL connection string.</param>
    public PostgreSqlGameStore(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    // ── helpers ──────────────────────────────────────────────────────────

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    private async Task EnsureInitializedAsync(CancellationToken ct)
    {
        if (_initialized) return;
        await _initLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_initialized) return;
            await EnsureTablesAsync(ct).ConfigureAwait(false);
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task EnsureTablesAsync(CancellationToken ct)
    {
        const string sql = @"
CREATE TABLE IF NOT EXISTS ""GameifyKit_Players"" (
    ""UserId""       VARCHAR(256) NOT NULL PRIMARY KEY,
    ""Xp""           BIGINT       NOT NULL DEFAULT 0,
    ""Level""        INT          NOT NULL DEFAULT 1,
    ""LastActivity"" TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS ""GameifyKit_Achievements"" (
    ""UserId""        VARCHAR(256) NOT NULL,
    ""AchievementId"" VARCHAR(256) NOT NULL,
    ""UnlockedAt""    TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    PRIMARY KEY (""UserId"", ""AchievementId"")
);

CREATE TABLE IF NOT EXISTS ""GameifyKit_QuestProgress"" (
    ""UserId""  VARCHAR(256) NOT NULL,
    ""QuestId"" VARCHAR(256) NOT NULL,
    ""StepId""  VARCHAR(256) NOT NULL,
    PRIMARY KEY (""UserId"", ""QuestId"", ""StepId"")
);

CREATE TABLE IF NOT EXISTS ""GameifyKit_ActiveQuests"" (
    ""UserId""    VARCHAR(256) NOT NULL,
    ""QuestId""   VARCHAR(256) NOT NULL,
    ""StartedAt"" TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    PRIMARY KEY (""UserId"", ""QuestId"")
);

CREATE TABLE IF NOT EXISTS ""GameifyKit_Streaks"" (
    ""UserId""         VARCHAR(256) NOT NULL,
    ""StreakId""        VARCHAR(256) NOT NULL,
    ""CurrentStreak""  INT          NOT NULL DEFAULT 0,
    ""BestStreak""     INT          NOT NULL DEFAULT 0,
    ""LastRecordedAt"" TIMESTAMPTZ  NULL,
    PRIMARY KEY (""UserId"", ""StreakId"")
);

CREATE TABLE IF NOT EXISTS ""GameifyKit_Leaderboard"" (
    ""UserId""      VARCHAR(256) NOT NULL,
    ""Period""      INT          NOT NULL,
    ""Xp""          BIGINT       NOT NULL DEFAULT 0,
    ""DisplayName"" VARCHAR(256) NULL,
    ""Level""       INT          NOT NULL DEFAULT 1,
    PRIMARY KEY (""UserId"", ""Period"")
);

CREATE TABLE IF NOT EXISTS ""GameifyKit_Boosts"" (
    ""Id""          VARCHAR(64)  NOT NULL PRIMARY KEY,
    ""UserId""      VARCHAR(256) NOT NULL,
    ""Multiplier""  DOUBLE PRECISION NOT NULL,
    ""DurationMs""  BIGINT       NOT NULL,
    ""Reason""      VARCHAR(512) NOT NULL DEFAULT '',
    ""ActivatedAt"" TIMESTAMPTZ  NOT NULL,
    ""ExpiresAt""   TIMESTAMPTZ  NOT NULL
);

CREATE TABLE IF NOT EXISTS ""GameifyKit_Wallet"" (
    ""UserId""         VARCHAR(256) NOT NULL PRIMARY KEY,
    ""Balance""        BIGINT       NOT NULL DEFAULT 0,
    ""LifetimeEarned"" BIGINT       NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS ""GameifyKit_Purchases"" (
    ""Id""          BIGSERIAL    PRIMARY KEY,
    ""UserId""      VARCHAR(256) NOT NULL,
    ""RewardId""    VARCHAR(256) NOT NULL,
    ""PurchasedAt"" TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS ""GameifyKit_Stats"" (
    ""UserId"" VARCHAR(256) NOT NULL,
    ""Key""    VARCHAR(256) NOT NULL,
    ""Value""  BIGINT       NOT NULL DEFAULT 0,
    PRIMARY KEY (""UserId"", ""Key"")
);

CREATE TABLE IF NOT EXISTS ""GameifyKit_Counters"" (
    ""UserId""  VARCHAR(256) NOT NULL,
    ""Counter"" VARCHAR(256) NOT NULL,
    ""Value""   BIGINT       NOT NULL DEFAULT 0,
    PRIMARY KEY (""UserId"", ""Counter"")
);

CREATE TABLE IF NOT EXISTS ""GameifyKit_Events"" (
    ""Id""        BIGSERIAL    PRIMARY KEY,
    ""UserId""    VARCHAR(256) NOT NULL,
    ""EventType"" VARCHAR(256) NOT NULL,
    ""Payload""   TEXT         NULL,
    ""CreatedAt"" TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);
";
        using var db = CreateConnection();
        await db.OpenAsync(ct).ConfigureAwait(false);
        await db.ExecuteAsync(new CommandDefinition(sql, cancellationToken: ct)).ConfigureAwait(false);
    }

    // ── Player ───────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<long> GetPlayerXpAsync(string userId, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        var xp = await db.QuerySingleOrDefaultAsync<long?>(
            new CommandDefinition(
                @"SELECT ""Xp"" FROM ""GameifyKit_Players"" WHERE ""UserId"" = @UserId",
                new { UserId = userId },
                cancellationToken: ct)).ConfigureAwait(false);
        return xp ?? 0;
    }

    /// <inheritdoc />
    public async Task SetPlayerXpAsync(string userId, long xp, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        await db.ExecuteAsync(new CommandDefinition(@"
INSERT INTO ""GameifyKit_Players"" (""UserId"", ""Xp"")
VALUES (@UserId, @Xp)
ON CONFLICT (""UserId"") DO UPDATE SET ""Xp"" = @Xp",
            new { UserId = userId, Xp = xp },
            cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> GetPlayerLevelAsync(string userId, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        var level = await db.QuerySingleOrDefaultAsync<int?>(
            new CommandDefinition(
                @"SELECT ""Level"" FROM ""GameifyKit_Players"" WHERE ""UserId"" = @UserId",
                new { UserId = userId },
                cancellationToken: ct)).ConfigureAwait(false);
        return level ?? 1;
    }

    /// <inheritdoc />
    public async Task SetPlayerLevelAsync(string userId, int level, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        await db.ExecuteAsync(new CommandDefinition(@"
INSERT INTO ""GameifyKit_Players"" (""UserId"", ""Level"")
VALUES (@UserId, @Level)
ON CONFLICT (""UserId"") DO UPDATE SET ""Level"" = @Level",
            new { UserId = userId, Level = level },
            cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RecordActivityAsync(string userId, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        await db.ExecuteAsync(new CommandDefinition(@"
INSERT INTO ""GameifyKit_Players"" (""UserId"", ""LastActivity"")
VALUES (@UserId, NOW())
ON CONFLICT (""UserId"") DO UPDATE SET ""LastActivity"" = NOW()",
            new { UserId = userId },
            cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<DateTimeOffset> GetLastActivityAsync(string userId, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        var result = await db.QuerySingleOrDefaultAsync<DateTimeOffset?>(
            new CommandDefinition(
                @"SELECT ""LastActivity"" FROM ""GameifyKit_Players"" WHERE ""UserId"" = @UserId",
                new { UserId = userId },
                cancellationToken: ct)).ConfigureAwait(false);
        return result ?? DateTimeOffset.UtcNow;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetAllPlayerIdsAsync(CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        var ids = await db.QueryAsync<string>(
            new CommandDefinition(
                @"SELECT ""UserId"" FROM ""GameifyKit_Players""",
                cancellationToken: ct)).ConfigureAwait(false);
        return ids.AsList();
    }

    // ── Counters & Stats ─────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task IncrementCounterAsync(string userId, string counter, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        await db.ExecuteAsync(new CommandDefinition(@"
INSERT INTO ""GameifyKit_Counters"" (""UserId"", ""Counter"", ""Value"")
VALUES (@UserId, @Counter, 1)
ON CONFLICT (""UserId"", ""Counter"") DO UPDATE SET ""Value"" = ""GameifyKit_Counters"".""Value"" + 1",
            new { UserId = userId, Counter = counter },
            cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, long>> GetCountersAsync(string userId, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        var rows = await db.QueryAsync<(string Counter, long Value)>(
            new CommandDefinition(
                @"SELECT ""Counter"", ""Value"" FROM ""GameifyKit_Counters"" WHERE ""UserId"" = @UserId",
                new { UserId = userId },
                cancellationToken: ct)).ConfigureAwait(false);
        return rows.ToDictionary(r => r.Counter, r => r.Value);
    }

    /// <inheritdoc />
    public async Task SetStatAsync(string userId, string key, long value, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        await db.ExecuteAsync(new CommandDefinition(@"
INSERT INTO ""GameifyKit_Stats"" (""UserId"", ""Key"", ""Value"")
VALUES (@UserId, @Key, @Value)
ON CONFLICT (""UserId"", ""Key"") DO UPDATE SET ""Value"" = @Value",
            new { UserId = userId, Key = key, Value = value },
            cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, long>> GetStatsAsync(string userId, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        var rows = await db.QueryAsync<(string Key, long Value)>(
            new CommandDefinition(
                @"SELECT ""Key"", ""Value"" FROM ""GameifyKit_Stats"" WHERE ""UserId"" = @UserId",
                new { UserId = userId },
                cancellationToken: ct)).ConfigureAwait(false);
        return rows.ToDictionary(r => r.Key, r => r.Value);
    }

    // ── Achievements ─────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task UnlockAchievementAsync(string userId, string achievementId, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        await db.ExecuteAsync(new CommandDefinition(@"
INSERT INTO ""GameifyKit_Achievements"" (""UserId"", ""AchievementId"", ""UnlockedAt"")
VALUES (@UserId, @AchievementId, NOW())
ON CONFLICT (""UserId"", ""AchievementId"") DO NOTHING",
            new { UserId = userId, AchievementId = achievementId },
            cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HashSet<string>> GetUnlockedAchievementIdsAsync(string userId, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        var ids = await db.QueryAsync<string>(
            new CommandDefinition(
                @"SELECT ""AchievementId"" FROM ""GameifyKit_Achievements"" WHERE ""UserId"" = @UserId",
                new { UserId = userId },
                cancellationToken: ct)).ConfigureAwait(false);
        return new HashSet<string>(ids);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UnlockedAchievementRecord>> GetUnlockedAchievementsAsync(string userId, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        var rows = await db.QueryAsync<UnlockedAchievementRecord>(
            new CommandDefinition(
                @"SELECT ""AchievementId"", ""UnlockedAt"" FROM ""GameifyKit_Achievements"" WHERE ""UserId"" = @UserId",
                new { UserId = userId },
                cancellationToken: ct)).ConfigureAwait(false);
        return rows.AsList();
    }

    // ── Quests ───────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task AssignQuestAsync(string userId, string questId, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        await db.ExecuteAsync(new CommandDefinition(@"
INSERT INTO ""GameifyKit_ActiveQuests"" (""UserId"", ""QuestId"", ""StartedAt"")
VALUES (@UserId, @QuestId, NOW())
ON CONFLICT (""UserId"", ""QuestId"") DO NOTHING",
            new { UserId = userId, QuestId = questId },
            cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task CompleteQuestStepAsync(string userId, string questId, string stepId, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        await db.ExecuteAsync(new CommandDefinition(@"
INSERT INTO ""GameifyKit_QuestProgress"" (""UserId"", ""QuestId"", ""StepId"")
VALUES (@UserId, @QuestId, @StepId)
ON CONFLICT (""UserId"", ""QuestId"", ""StepId"") DO NOTHING",
            new { UserId = userId, QuestId = questId, StepId = stepId },
            cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetCompletedQuestStepsAsync(string userId, string questId, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        var ids = await db.QueryAsync<string>(
            new CommandDefinition(
                @"SELECT ""StepId"" FROM ""GameifyKit_QuestProgress"" WHERE ""UserId"" = @UserId AND ""QuestId"" = @QuestId",
                new { UserId = userId, QuestId = questId },
                cancellationToken: ct)).ConfigureAwait(false);
        return ids.AsList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetActiveQuestIdsAsync(string userId, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        var ids = await db.QueryAsync<string>(
            new CommandDefinition(
                @"SELECT ""QuestId"" FROM ""GameifyKit_ActiveQuests"" WHERE ""UserId"" = @UserId",
                new { UserId = userId },
                cancellationToken: ct)).ConfigureAwait(false);
        return ids.AsList();
    }

    /// <inheritdoc />
    public async Task<DateTimeOffset> GetQuestStartTimeAsync(string userId, string questId, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        var result = await db.QuerySingleOrDefaultAsync<DateTimeOffset?>(
            new CommandDefinition(
                @"SELECT ""StartedAt"" FROM ""GameifyKit_ActiveQuests"" WHERE ""UserId"" = @UserId AND ""QuestId"" = @QuestId",
                new { UserId = userId, QuestId = questId },
                cancellationToken: ct)).ConfigureAwait(false);
        return result ?? DateTimeOffset.UtcNow;
    }

    // ── Streaks ──────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<StreakDataRecord> GetStreakDataAsync(string userId, string streakId, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        var row = await db.QuerySingleOrDefaultAsync<StreakDataRecord>(
            new CommandDefinition(
                @"SELECT ""CurrentStreak"", ""BestStreak"", ""LastRecordedAt"" FROM ""GameifyKit_Streaks"" WHERE ""UserId"" = @UserId AND ""StreakId"" = @StreakId",
                new { UserId = userId, StreakId = streakId },
                cancellationToken: ct)).ConfigureAwait(false);
        return row ?? new StreakDataRecord();
    }

    /// <inheritdoc />
    public async Task SetStreakDataAsync(string userId, string streakId, int current, int best, DateTimeOffset lastRecorded, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        await db.ExecuteAsync(new CommandDefinition(@"
INSERT INTO ""GameifyKit_Streaks"" (""UserId"", ""StreakId"", ""CurrentStreak"", ""BestStreak"", ""LastRecordedAt"")
VALUES (@UserId, @StreakId, @Current, @Best, @LastRecorded)
ON CONFLICT (""UserId"", ""StreakId"") DO UPDATE
    SET ""CurrentStreak"" = @Current, ""BestStreak"" = @Best, ""LastRecordedAt"" = @LastRecorded",
            new { UserId = userId, StreakId = streakId, Current = current, Best = best, LastRecorded = lastRecorded },
            cancellationToken: ct)).ConfigureAwait(false);
    }

    // ── Leaderboard ──────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<IReadOnlyList<LeaderboardEntryData>> GetLeaderboardEntriesAsync(LeaderboardPeriod period, int count, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        var rows = await db.QueryAsync<LeaderboardEntryData>(
            new CommandDefinition(
                @"SELECT ""UserId"", ""DisplayName"", ""Xp"", ""Level"" FROM ""GameifyKit_Leaderboard"" WHERE ""Period"" = @Period ORDER BY ""Xp"" DESC LIMIT @Count",
                new { Period = (int)period, Count = count },
                cancellationToken: ct)).ConfigureAwait(false);
        return rows.AsList();
    }

    /// <inheritdoc />
    public async Task UpdateLeaderboardAsync(string userId, LeaderboardPeriod period, long xp, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        await db.ExecuteAsync(new CommandDefinition(@"
INSERT INTO ""GameifyKit_Leaderboard"" (""UserId"", ""Period"", ""Xp"")
VALUES (@UserId, @Period, @Xp)
ON CONFLICT (""UserId"", ""Period"") DO UPDATE SET ""Xp"" = @Xp",
            new { UserId = userId, Period = (int)period, Xp = xp },
            cancellationToken: ct)).ConfigureAwait(false);
    }

    // ── Boosts ───────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task AddBoostAsync(string userId, XpBoost boost, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        await db.ExecuteAsync(new CommandDefinition(@"
INSERT INTO ""GameifyKit_Boosts"" (""Id"", ""UserId"", ""Multiplier"", ""DurationMs"", ""Reason"", ""ActivatedAt"", ""ExpiresAt"")
VALUES (@Id, @UserId, @Multiplier, @DurationMs, @Reason, @ActivatedAt, @ExpiresAt)",
            new
            {
                boost.Id,
                UserId = userId,
                boost.Multiplier,
                DurationMs = (long)boost.Duration.TotalMilliseconds,
                boost.Reason,
                boost.ActivatedAt,
                ExpiresAt = boost.ExpiresAt
            },
            cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<XpBoost>> GetActiveBoostsAsync(string userId, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        var rows = await db.QueryAsync<BoostRow>(
            new CommandDefinition(
                @"SELECT ""Id"", ""Multiplier"", ""DurationMs"", ""Reason"", ""ActivatedAt"", ""ExpiresAt"" FROM ""GameifyKit_Boosts"" WHERE ""UserId"" = @UserId AND ""ExpiresAt"" > NOW()",
                new { UserId = userId },
                cancellationToken: ct)).ConfigureAwait(false);

        return rows.Select(r => new XpBoost
        {
            Id = r.Id,
            Multiplier = r.Multiplier,
            Duration = TimeSpan.FromMilliseconds(r.DurationMs),
            Reason = r.Reason,
            ActivatedAt = r.ActivatedAt
        }).ToList();
    }

    // ── Economy ──────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<WalletData> GetWalletAsync(string userId, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        var row = await db.QuerySingleOrDefaultAsync<WalletData>(
            new CommandDefinition(
                @"SELECT ""Balance"", ""LifetimeEarned"" FROM ""GameifyKit_Wallet"" WHERE ""UserId"" = @UserId",
                new { UserId = userId },
                cancellationToken: ct)).ConfigureAwait(false);
        return row ?? new WalletData();
    }

    /// <inheritdoc />
    public async Task SetWalletBalanceAsync(string userId, long balance, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        await db.ExecuteAsync(new CommandDefinition(@"
INSERT INTO ""GameifyKit_Wallet"" (""UserId"", ""Balance"")
VALUES (@UserId, @Balance)
ON CONFLICT (""UserId"") DO UPDATE SET ""Balance"" = @Balance",
            new { UserId = userId, Balance = balance },
            cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task AddLifetimeEarnedAsync(string userId, long amount, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        await db.ExecuteAsync(new CommandDefinition(@"
INSERT INTO ""GameifyKit_Wallet"" (""UserId"", ""LifetimeEarned"")
VALUES (@UserId, @Amount)
ON CONFLICT (""UserId"") DO UPDATE SET ""LifetimeEarned"" = ""GameifyKit_Wallet"".""LifetimeEarned"" + @Amount",
            new { UserId = userId, Amount = amount },
            cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RecordPurchaseAsync(string userId, string rewardId, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        await db.ExecuteAsync(new CommandDefinition(
            @"INSERT INTO ""GameifyKit_Purchases"" (""UserId"", ""RewardId"", ""PurchasedAt"") VALUES (@UserId, @RewardId, NOW())",
            new { UserId = userId, RewardId = rewardId },
            cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> HasPurchasedAsync(string userId, string rewardId, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        var exists = await db.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                @"SELECT EXISTS (SELECT 1 FROM ""GameifyKit_Purchases"" WHERE ""UserId"" = @UserId AND ""RewardId"" = @RewardId)",
                new { UserId = userId, RewardId = rewardId },
                cancellationToken: ct)).ConfigureAwait(false);
        return exists;
    }

    /// <inheritdoc />
    public async Task<int> GetDailyPurchaseCountAsync(string userId, string rewardId, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        return await db.ExecuteScalarAsync<int>(
            new CommandDefinition(
                @"SELECT COUNT(*) FROM ""GameifyKit_Purchases"" WHERE ""UserId"" = @UserId AND ""RewardId"" = @RewardId AND ""PurchasedAt""::date = CURRENT_DATE",
                new { UserId = userId, RewardId = rewardId },
                cancellationToken: ct)).ConfigureAwait(false);
    }

    // ── Internal DTO for Boost rows ──────────────────────────────────────

    private sealed class BoostRow
    {
        public string Id { get; set; } = string.Empty;
        public double Multiplier { get; set; }
        public long DurationMs { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTimeOffset ActivatedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
    }
}
