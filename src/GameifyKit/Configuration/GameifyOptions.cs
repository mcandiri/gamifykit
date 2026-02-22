using GameifyKit.Achievements;
using GameifyKit.Boosts;
using GameifyKit.Events;
using GameifyKit.Quests;
using GameifyKit.Storage;
using GameifyKit.Streaks;

namespace GameifyKit.Configuration;

/// <summary>
/// Master configuration for GameifyKit.
/// </summary>
public sealed class GameifyOptions
{
    /// <summary>Leveling system configuration.</summary>
    public LevelingConfig Leveling { get; } = new();

    /// <summary>Leaderboard configuration.</summary>
    public LeaderboardConfig Leaderboard { get; } = new();

    /// <summary>Economy configuration.</summary>
    public EconomyConfig Economy { get; } = new();

    /// <summary>Anti-cheat rules configuration.</summary>
    public RulesConfig Rules { get; } = new();

    /// <summary>Whether analytics is enabled.</summary>
    public bool AnalyticsEnabled { get; set; }

    /// <summary>Boosts configuration.</summary>
    public BoostsConfig Boosts { get; } = new();

    /// <summary>Achievement definitions.</summary>
    public List<AchievementDefinition> Achievements { get; } = new();

    /// <summary>Quest definitions.</summary>
    public List<QuestDefinition> Quests { get; } = new();

    /// <summary>Streak definitions.</summary>
    public List<StreakDefinition> Streaks { get; } = new();

    /// <summary>Event handler registrations.</summary>
    internal List<(Type EventType, Delegate Handler)> EventHandlers { get; } = new();

    /// <summary>Factory for creating the game store.</summary>
    internal Func<IServiceProvider, IGameStore>? StoreFactory { get; set; }

    /// <summary>Use the in-memory store for testing and prototyping.</summary>
    public void UseInMemoryStore()
    {
        StoreFactory = _ => new InMemoryGameStore();
    }

    /// <summary>Use SQL Server for storage.</summary>
    public void UseSqlServer(string connectionString)
    {
        ArgumentNullException.ThrowIfNull(connectionString);
        StoreFactory = _ => new SqlServerGameStore(connectionString);
    }

    /// <summary>Use PostgreSQL for storage.</summary>
    public void UsePostgreSql(string connectionString)
    {
        ArgumentNullException.ThrowIfNull(connectionString);
        StoreFactory = _ => new PostgreSqlGameStore(connectionString);
    }

    /// <summary>Configure the leveling system.</summary>
    public void ConfigureLeveling(Action<LevelingConfig> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(Leveling);
    }

    /// <summary>Configure achievements.</summary>
    public void ConfigureAchievements(Action<AchievementBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new AchievementBuilder(Achievements);
        configure(builder);
    }

    /// <summary>Configure quests.</summary>
    public void ConfigureQuests(Action<QuestBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new QuestBuilder(Quests);
        configure(builder);
    }

    /// <summary>Configure streaks.</summary>
    public void ConfigureStreaks(Action<StreakBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new StreakBuilder(Streaks);
        configure(builder);
    }

    /// <summary>Configure the leaderboard.</summary>
    public void ConfigureLeaderboard(Action<LeaderboardConfig> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(Leaderboard);
    }

    /// <summary>Configure boosts.</summary>
    public void ConfigureBoosts(Action<BoostsConfig> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(Boosts);
    }

    /// <summary>Configure the economy.</summary>
    public void ConfigureEconomy(Action<EconomyConfig> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(Economy);
    }

    /// <summary>Configure anti-cheat rules.</summary>
    public void ConfigureRules(Action<RulesConfig> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(Rules);
    }

    /// <summary>Enable the analytics engine.</summary>
    public void EnableAnalytics()
    {
        AnalyticsEnabled = true;
    }

    /// <summary>Register an event handler.</summary>
    public void OnEvent<TEvent>(Func<TEvent, Task> handler) where TEvent : GameEvent
    {
        ArgumentNullException.ThrowIfNull(handler);
        EventHandlers.Add((typeof(TEvent), handler));
    }
}

/// <summary>
/// Configuration for the boost system.
/// </summary>
public sealed class BoostsConfig
{
    /// <summary>Maximum number of boosts that can stack.</summary>
    public int MaxStackableBoosts { get; set; } = 3;

    /// <summary>Maximum combined multiplier cap.</summary>
    public double MaxMultiplier { get; set; } = 5.0;
}
