using GameifyKit.Achievements;
using GameifyKit.Analytics;
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameifyKit.Extensions;

/// <summary>
/// Extension methods for registering GameifyKit services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds GameifyKit services to the dependency injection container.
    /// </summary>
    public static IServiceCollection AddGameifyKit(this IServiceCollection services, Action<GameifyOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new GameifyOptions();
        configure(options);

        // Storage
        if (options.StoreFactory != null)
        {
            services.AddSingleton<IGameStore>(sp => options.StoreFactory(sp));
        }
        else
        {
            services.AddSingleton<IGameStore>(new InMemoryGameStore());
        }

        // Event Bus with pre-registered handlers
        services.AddSingleton<IGameEventBus>(sp =>
        {
            var logger = sp.GetService<ILogger<GameEventBus>>() ?? NullLogger<GameEventBus>.Instance;
            var bus = new GameEventBus(logger);

            foreach (var (eventType, handler) in options.EventHandlers)
            {
                var method = typeof(IGameEventBus).GetMethod(nameof(IGameEventBus.Subscribe))!
                    .MakeGenericMethod(eventType);
                method.Invoke(bus, [handler]);
            }

            return bus;
        });

        // Configuration singletons
        services.AddSingleton(options.Leveling);
        services.AddSingleton(options.Leaderboard);
        services.AddSingleton(options.Economy);
        services.AddSingleton(options.Rules);
        services.AddSingleton(options.Boosts);
        services.AddSingleton<IReadOnlyList<AchievementDefinition>>(options.Achievements);
        services.AddSingleton<IReadOnlyList<QuestDefinition>>(options.Quests);
        services.AddSingleton<IReadOnlyList<StreakDefinition>>(options.Streaks);

        // Engines
        services.AddSingleton<IRuleEngine, RuleEngine>();
        services.AddSingleton<IBoostEngine, BoostEngine>();
        services.AddSingleton<IXpEngine, XpEngine>();
        services.AddSingleton<IAchievementEngine, AchievementEngine>();
        services.AddSingleton<IQuestEngine, QuestEngine>();
        services.AddSingleton<IStreakEngine, StreakEngine>();
        services.AddSingleton<ILeaderboardEngine, LeaderboardEngine>();
        services.AddSingleton<IEconomyEngine, EconomyEngine>();

        if (options.AnalyticsEnabled)
        {
            services.AddSingleton<IAnalyticsEngine, AnalyticsEngine>();
        }
        else
        {
            services.AddSingleton<IAnalyticsEngine, NoOpAnalyticsEngine>();
        }

        // Main engine
        services.AddSingleton<IGameEngine, GameEngine>();

        return services;
    }
}

/// <summary>
/// No-op analytics engine when analytics is disabled.
/// </summary>
internal sealed class NoOpAnalyticsEngine : IAnalyticsEngine
{
    public Task<EngagementInsights> GetInsightsAsync(CancellationToken ct = default)
        => Task.FromResult(new EngagementInsights());

    public Task<IReadOnlyList<TierDistribution>> GetTierDistributionAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<TierDistribution>>([]);

    public Task<IReadOnlyList<QuestAnalytics>> GetQuestAnalyticsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<QuestAnalytics>>([]);
}
