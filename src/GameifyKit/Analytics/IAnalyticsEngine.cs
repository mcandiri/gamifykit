namespace GameifyKit.Analytics;

/// <summary>
/// Engine for engagement analytics.
/// </summary>
public interface IAnalyticsEngine
{
    /// <summary>
    /// Gets overall engagement insights.
    /// </summary>
    Task<EngagementInsights> GetInsightsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets tier distribution analytics.
    /// </summary>
    Task<IReadOnlyList<TierDistribution>> GetTierDistributionAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets quest analytics.
    /// </summary>
    Task<IReadOnlyList<QuestAnalytics>> GetQuestAnalyticsAsync(CancellationToken ct = default);
}
