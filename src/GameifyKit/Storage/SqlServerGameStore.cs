using System.Data;
using Dapper;
using GameifyKit.Boosts;
using GameifyKit.Leaderboard;
using Microsoft.Data.SqlClient;

namespace GameifyKit.Storage;

/// <summary>
/// SQL Server implementation of <see cref="IGameStore"/> using Dapper and Microsoft.Data.SqlClient.
/// Tables are auto-created on first use with the <c>GameifyKit_</c> prefix.
/// This class is thread-safe; each call opens its own connection from the pool.
/// </summary>
public sealed class SqlServerGameStore : IGameStore
{
    private readonly string _connectionString;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private volatile bool _initialized;

    /// <summary>
    /// Initializes a new instance of <see cref="SqlServerGameStore"/>.
    /// </summary>
    /// <param name="connectionString">SQL Server connection string.</param>
    public SqlServerGameStore(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    // ── helpers ──────────────────────────────────────────────────────────

    private SqlConnection CreateConnection() => new(_connectionString);

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
IF OBJECT_ID(N'GameifyKit_Players', N'U') IS NULL
CREATE TABLE GameifyKit_Players (
    UserId        NVARCHAR(256) NOT NULL PRIMARY KEY,
    Xp            BIGINT        NOT NULL DEFAULT 0,
    [Level]       INT           NOT NULL DEFAULT 1,
    LastActivity  DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
);

IF OBJECT_ID(N'GameifyKit_Achievements', N'U') IS NULL
CREATE TABLE GameifyKit_Achievements (
    UserId        NVARCHAR(256) NOT NULL,
    AchievementId NVARCHAR(256) NOT NULL,
    UnlockedAt    DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CONSTRAINT PK_GameifyKit_Achievements PRIMARY KEY (UserId, AchievementId)
);

IF OBJECT_ID(N'GameifyKit_QuestProgress', N'U') IS NULL
CREATE TABLE GameifyKit_QuestProgress (
    UserId    NVARCHAR(256) NOT NULL,
    QuestId   NVARCHAR(256) NOT NULL,
    StepId    NVARCHAR(256) NOT NULL,
    CONSTRAINT PK_GameifyKit_QuestProgress PRIMARY KEY (UserId, QuestId, StepId)
);

IF OBJECT_ID(N'GameifyKit_ActiveQuests', N'U') IS NULL
CREATE TABLE GameifyKit_ActiveQuests (
    UserId    NVARCHAR(256) NOT NULL,
    QuestId   NVARCHAR(256) NOT NULL,
    StartedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CONSTRAINT PK_GameifyKit_ActiveQuests PRIMARY KEY (UserId, QuestId)
);

IF OBJECT_ID(N'GameifyKit_Streaks', N'U') IS NULL
CREATE TABLE GameifyKit_Streaks (
    UserId         NVARCHAR(256) NOT NULL,
    StreakId        NVARCHAR(256) NOT NULL,
    CurrentStreak  INT           NOT NULL DEFAULT 0,
    BestStreak     INT           NOT NULL DEFAULT 0,
    LastRecordedAt DATETIMEOFFSET NULL,
    CONSTRAINT PK_GameifyKit_Streaks PRIMARY KEY (UserId, StreakId)
);

IF OBJECT_ID(N'GameifyKit_Leaderboard', N'U') IS NULL
CREATE TABLE GameifyKit_Leaderboard (
    UserId      NVARCHAR(256) NOT NULL,
    Period      INT           NOT NULL,
    Xp          BIGINT        NOT NULL DEFAULT 0,
    DisplayName NVARCHAR(256) NULL,
    [Level]     INT           NOT NULL DEFAULT 1,
    CONSTRAINT PK_GameifyKit_Leaderboard PRIMARY KEY (UserId, Period)
);

IF OBJECT_ID(N'GameifyKit_Boosts', N'U') IS NULL
CREATE TABLE GameifyKit_Boosts (
    Id          NVARCHAR(64)  NOT NULL PRIMARY KEY,
    UserId      NVARCHAR(256) NOT NULL,
    Multiplier  FLOAT         NOT NULL,
    DurationMs  BIGINT        NOT NULL,
    Reason      NVARCHAR(512) NOT NULL DEFAULT '',
    ActivatedAt DATETIMEOFFSET NOT NULL,
    ExpiresAt   DATETIMEOFFSET NOT NULL
);

IF OBJECT_ID(N'GameifyKit_Wallet', N'U') IS NULL
CREATE TABLE GameifyKit_Wallet (
    UserId         NVARCHAR(256) NOT NULL PRIMARY KEY,
    Balance        BIGINT        NOT NULL DEFAULT 0,
    LifetimeEarned BIGINT        NOT NULL DEFAULT 0
);

IF OBJECT_ID(N'GameifyKit_Purchases', N'U') IS NULL
CREATE TABLE GameifyKit_Purchases (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    UserId      NVARCHAR(256) NOT NULL,
    RewardId    NVARCHAR(256) NOT NULL,
    PurchasedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
);

IF OBJECT_ID(N'GameifyKit_Stats', N'U') IS NULL
CREATE TABLE GameifyKit_Stats (
    UserId NVARCHAR(256) NOT NULL,
    [Key]  NVARCHAR(256) NOT NULL,
    Value  BIGINT        NOT NULL DEFAULT 0,
    CONSTRAINT PK_GameifyKit_Stats PRIMARY KEY (UserId, [Key])
);

IF OBJECT_ID(N'GameifyKit_Counters', N'U') IS NULL
CREATE TABLE GameifyKit_Counters (
    UserId  NVARCHAR(256) NOT NULL,
    Counter NVARCHAR(256) NOT NULL,
    Value   BIGINT        NOT NULL DEFAULT 0,
    CONSTRAINT PK_GameifyKit_Counters PRIMARY KEY (UserId, Counter)
);

IF OBJECT_ID(N'GameifyKit_Events', N'U') IS NULL
CREATE TABLE GameifyKit_Events (
    Id        BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId    NVARCHAR(256) NOT NULL,
    EventType NVARCHAR(256) NOT NULL,
    Payload   NVARCHAR(MAX) NULL,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
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
                "SELECT Xp FROM GameifyKit_Players WHERE UserId = @UserId",
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
MERGE GameifyKit_Players AS tgt
USING (SELECT @UserId AS UserId) AS src ON tgt.UserId = src.UserId
WHEN MATCHED THEN UPDATE SET Xp = @Xp
WHEN NOT MATCHED THEN INSERT (UserId, Xp) VALUES (@UserId, @Xp);",
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
                "SELECT [Level] FROM GameifyKit_Players WHERE UserId = @UserId",
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
MERGE GameifyKit_Players AS tgt
USING (SELECT @UserId AS UserId) AS src ON tgt.UserId = src.UserId
WHEN MATCHED THEN UPDATE SET [Level] = @Level
WHEN NOT MATCHED THEN INSERT (UserId, [Level]) VALUES (@UserId, @Level);",
            new { UserId = userId, Level = level },
            cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RecordActivityAsync(string userId, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        await db.ExecuteAsync(new CommandDefinition(@"
MERGE GameifyKit_Players AS tgt
USING (SELECT @UserId AS UserId) AS src ON tgt.UserId = src.UserId
WHEN MATCHED THEN UPDATE SET LastActivity = SYSDATETIMEOFFSET()
WHEN NOT MATCHED THEN INSERT (UserId, LastActivity) VALUES (@UserId, SYSDATETIMEOFFSET());",
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
                "SELECT LastActivity FROM GameifyKit_Players WHERE UserId = @UserId",
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
                "SELECT UserId FROM GameifyKit_Players",
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
MERGE GameifyKit_Counters AS tgt
USING (SELECT @UserId AS UserId, @Counter AS Counter) AS src
    ON tgt.UserId = src.UserId AND tgt.Counter = src.Counter
WHEN MATCHED THEN UPDATE SET Value = tgt.Value + 1
WHEN NOT MATCHED THEN INSERT (UserId, Counter, Value) VALUES (@UserId, @Counter, 1);",
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
                "SELECT Counter, Value FROM GameifyKit_Counters WHERE UserId = @UserId",
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
MERGE GameifyKit_Stats AS tgt
USING (SELECT @UserId AS UserId, @Key AS [Key]) AS src
    ON tgt.UserId = src.UserId AND tgt.[Key] = src.[Key]
WHEN MATCHED THEN UPDATE SET Value = @Value
WHEN NOT MATCHED THEN INSERT (UserId, [Key], Value) VALUES (@UserId, @Key, @Value);",
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
                "SELECT [Key], Value FROM GameifyKit_Stats WHERE UserId = @UserId",
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
MERGE GameifyKit_Achievements AS tgt
USING (SELECT @UserId AS UserId, @AchievementId AS AchievementId) AS src
    ON tgt.UserId = src.UserId AND tgt.AchievementId = src.AchievementId
WHEN NOT MATCHED THEN INSERT (UserId, AchievementId, UnlockedAt)
    VALUES (@UserId, @AchievementId, SYSDATETIMEOFFSET());",
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
                "SELECT AchievementId FROM GameifyKit_Achievements WHERE UserId = @UserId",
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
                "SELECT AchievementId, UnlockedAt FROM GameifyKit_Achievements WHERE UserId = @UserId",
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
MERGE GameifyKit_ActiveQuests AS tgt
USING (SELECT @UserId AS UserId, @QuestId AS QuestId) AS src
    ON tgt.UserId = src.UserId AND tgt.QuestId = src.QuestId
WHEN NOT MATCHED THEN INSERT (UserId, QuestId, StartedAt)
    VALUES (@UserId, @QuestId, SYSDATETIMEOFFSET());",
            new { UserId = userId, QuestId = questId },
            cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task CompleteQuestStepAsync(string userId, string questId, string stepId, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        await db.ExecuteAsync(new CommandDefinition(@"
MERGE GameifyKit_QuestProgress AS tgt
USING (SELECT @UserId AS UserId, @QuestId AS QuestId, @StepId AS StepId) AS src
    ON tgt.UserId = src.UserId AND tgt.QuestId = src.QuestId AND tgt.StepId = src.StepId
WHEN NOT MATCHED THEN INSERT (UserId, QuestId, StepId)
    VALUES (@UserId, @QuestId, @StepId);",
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
                "SELECT StepId FROM GameifyKit_QuestProgress WHERE UserId = @UserId AND QuestId = @QuestId",
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
                "SELECT QuestId FROM GameifyKit_ActiveQuests WHERE UserId = @UserId",
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
                "SELECT StartedAt FROM GameifyKit_ActiveQuests WHERE UserId = @UserId AND QuestId = @QuestId",
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
                "SELECT CurrentStreak, BestStreak, LastRecordedAt FROM GameifyKit_Streaks WHERE UserId = @UserId AND StreakId = @StreakId",
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
MERGE GameifyKit_Streaks AS tgt
USING (SELECT @UserId AS UserId, @StreakId AS StreakId) AS src
    ON tgt.UserId = src.UserId AND tgt.StreakId = src.StreakId
WHEN MATCHED THEN UPDATE SET CurrentStreak = @Current, BestStreak = @Best, LastRecordedAt = @LastRecorded
WHEN NOT MATCHED THEN INSERT (UserId, StreakId, CurrentStreak, BestStreak, LastRecordedAt)
    VALUES (@UserId, @StreakId, @Current, @Best, @LastRecorded);",
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
                "SELECT TOP(@Count) UserId, DisplayName, Xp, [Level] FROM GameifyKit_Leaderboard WHERE Period = @Period ORDER BY Xp DESC",
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
MERGE GameifyKit_Leaderboard AS tgt
USING (SELECT @UserId AS UserId, @Period AS Period) AS src
    ON tgt.UserId = src.UserId AND tgt.Period = src.Period
WHEN MATCHED THEN UPDATE SET Xp = @Xp
WHEN NOT MATCHED THEN INSERT (UserId, Period, Xp)
    VALUES (@UserId, @Period, @Xp);",
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
INSERT INTO GameifyKit_Boosts (Id, UserId, Multiplier, DurationMs, Reason, ActivatedAt, ExpiresAt)
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
                "SELECT Id, Multiplier, DurationMs, Reason, ActivatedAt, ExpiresAt FROM GameifyKit_Boosts WHERE UserId = @UserId AND ExpiresAt > SYSDATETIMEOFFSET()",
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
                "SELECT Balance, LifetimeEarned FROM GameifyKit_Wallet WHERE UserId = @UserId",
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
MERGE GameifyKit_Wallet AS tgt
USING (SELECT @UserId AS UserId) AS src ON tgt.UserId = src.UserId
WHEN MATCHED THEN UPDATE SET Balance = @Balance
WHEN NOT MATCHED THEN INSERT (UserId, Balance) VALUES (@UserId, @Balance);",
            new { UserId = userId, Balance = balance },
            cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task AddLifetimeEarnedAsync(string userId, long amount, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        await db.ExecuteAsync(new CommandDefinition(@"
MERGE GameifyKit_Wallet AS tgt
USING (SELECT @UserId AS UserId) AS src ON tgt.UserId = src.UserId
WHEN MATCHED THEN UPDATE SET LifetimeEarned = tgt.LifetimeEarned + @Amount
WHEN NOT MATCHED THEN INSERT (UserId, LifetimeEarned) VALUES (@UserId, @Amount);",
            new { UserId = userId, Amount = amount },
            cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RecordPurchaseAsync(string userId, string rewardId, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        await db.ExecuteAsync(new CommandDefinition(
            "INSERT INTO GameifyKit_Purchases (UserId, RewardId, PurchasedAt) VALUES (@UserId, @RewardId, SYSDATETIMEOFFSET())",
            new { UserId = userId, RewardId = rewardId },
            cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> HasPurchasedAsync(string userId, string rewardId, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        var count = await db.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT CASE WHEN EXISTS (SELECT 1 FROM GameifyKit_Purchases WHERE UserId = @UserId AND RewardId = @RewardId) THEN 1 ELSE 0 END",
                new { UserId = userId, RewardId = rewardId },
                cancellationToken: ct)).ConfigureAwait(false);
        return count == 1;
    }

    /// <inheritdoc />
    public async Task<int> GetDailyPurchaseCountAsync(string userId, string rewardId, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        using var db = CreateConnection();
        return await db.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT COUNT(*) FROM GameifyKit_Purchases WHERE UserId = @UserId AND RewardId = @RewardId AND CAST(PurchasedAt AS DATE) = CAST(SYSDATETIMEOFFSET() AS DATE)",
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
