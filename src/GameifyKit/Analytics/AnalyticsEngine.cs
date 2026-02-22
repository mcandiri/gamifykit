using GameifyKit.Storage;
using Microsoft.Extensions.Logging;

namespace GameifyKit.Analytics;

/// <summary>
/// Default implementation of the analytics engine.
/// </summary>
public sealed class AnalyticsEngine : IAnalyticsEngine
{
    private readonly IGameStore _store;
    private readonly ILogger<AnalyticsEngine> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnalyticsEngine"/> class.
    /// </summary>
    public AnalyticsEngine(IGameStore store, ILogger<AnalyticsEngine> logger)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<EngagementInsights> GetInsightsAsync(CancellationToken ct = default)
    {
        var allPlayers = await _store.GetAllPlayerIdsAsync(ct);
        var now = DateTimeOffset.UtcNow;

        int dau = 0;
        int wau = 0;
        long totalXp = 0;
        int totalLevels = 0;

        foreach (var playerId in allPlayers)
        {
            var lastActivity = await _store.GetLastActivityAsync(playerId, ct);
            var xp = await _store.GetPlayerXpAsync(playerId, ct);
            var level = await _store.GetPlayerLevelAsync(playerId, ct);

            if ((now - lastActivity).TotalDays <= 1) dau++;
            if ((now - lastActivity).TotalDays <= 7) wau++;

            totalXp += xp;
            totalLevels += level;
        }

        int total = allPlayers.Count;

        return new EngagementInsights
        {
            TotalPlayers = total,
            DailyActiveUsers = dau,
            WeeklyActiveUsers = wau,
            AverageXpPerPlayer = total > 0 ? (double)totalXp / total : 0,
            AverageLevel = total > 0 ? (double)totalLevels / total : 0
        };
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TierDistribution>> GetTierDistributionAsync(CancellationToken ct = default)
    {
        // Placeholder — would use leaderboard data in a full implementation
        return Task.FromResult<IReadOnlyList<TierDistribution>>(new List<TierDistribution>());
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<QuestAnalytics>> GetQuestAnalyticsAsync(CancellationToken ct = default)
    {
        // Placeholder — would aggregate quest progress in a full implementation
        return Task.FromResult<IReadOnlyList<QuestAnalytics>>(new List<QuestAnalytics>());
    }
}
