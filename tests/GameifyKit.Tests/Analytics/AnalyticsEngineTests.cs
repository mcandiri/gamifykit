using FluentAssertions;
using GameifyKit.Analytics;
using Xunit;
using GameifyKit.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameifyKit.Tests.Analytics;

public class AnalyticsEngineTests
{
    private readonly InMemoryGameStore _store;
    private readonly AnalyticsEngine _engine;

    public AnalyticsEngineTests()
    {
        _store = new InMemoryGameStore();
        _engine = new AnalyticsEngine(_store, NullLogger<AnalyticsEngine>.Instance);
    }

    [Fact]
    public async Task GetInsightsAsync_NoPlayers_ShouldReturnZeros()
    {
        var insights = await _engine.GetInsightsAsync();

        insights.TotalPlayers.Should().Be(0);
        insights.DailyActiveUsers.Should().Be(0);
        insights.WeeklyActiveUsers.Should().Be(0);
        insights.AverageXpPerPlayer.Should().Be(0);
        insights.AverageLevel.Should().Be(0);
    }

    [Fact]
    public async Task GetInsightsAsync_ShouldCountTotalPlayers()
    {
        // Create 3 players by setting their XP (which registers them in GetAllPlayerIdsAsync)
        await _store.SetPlayerXpAsync("user1", 100);
        await _store.SetPlayerXpAsync("user2", 200);
        await _store.SetPlayerXpAsync("user3", 300);

        var insights = await _engine.GetInsightsAsync();

        insights.TotalPlayers.Should().Be(3);
    }

    [Fact]
    public async Task GetInsightsAsync_ShouldCalculateAverageXp()
    {
        await _store.SetPlayerXpAsync("user1", 100);
        await _store.SetPlayerXpAsync("user2", 200);
        await _store.SetPlayerXpAsync("user3", 300);

        var insights = await _engine.GetInsightsAsync();

        insights.AverageXpPerPlayer.Should().Be(200.0); // (100+200+300)/3
    }

    [Fact]
    public async Task GetInsightsAsync_ShouldCalculateAverageLevel()
    {
        await _store.SetPlayerXpAsync("user1", 0);
        await _store.SetPlayerLevelAsync("user1", 5);
        await _store.SetPlayerXpAsync("user2", 0);
        await _store.SetPlayerLevelAsync("user2", 10);

        var insights = await _engine.GetInsightsAsync();

        insights.AverageLevel.Should().Be(7.5); // (5+10)/2
    }

    [Fact]
    public async Task GetInsightsAsync_ActivePlayers_ShouldCountAsDAU()
    {
        // Create players (SetPlayerXpAsync registers them in allPlayerIds)
        await _store.SetPlayerXpAsync("user1", 100);
        await _store.SetPlayerXpAsync("user2", 200);

        // Record activity for user1 (recent activity)
        await _store.RecordActivityAsync("user1");

        var insights = await _engine.GetInsightsAsync();

        // Both users should be DAU since GetLastActivityAsync returns UtcNow for new users
        insights.DailyActiveUsers.Should().BeGreaterThanOrEqualTo(1);
        insights.WeeklyActiveUsers.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetInsightsAsync_WithRecentActivity_AllShouldBeWeeklyActive()
    {
        await _store.SetPlayerXpAsync("user1", 100);
        await _store.RecordActivityAsync("user1");
        await _store.SetPlayerXpAsync("user2", 200);
        await _store.RecordActivityAsync("user2");

        var insights = await _engine.GetInsightsAsync();

        insights.WeeklyActiveUsers.Should().Be(2);
    }

    [Fact]
    public async Task GetTierDistributionAsync_ShouldReturnEmptyList()
    {
        // Current implementation returns empty list (placeholder)
        var distribution = await _engine.GetTierDistributionAsync();

        distribution.Should().BeEmpty();
    }

    [Fact]
    public async Task GetQuestAnalyticsAsync_ShouldReturnEmptyList()
    {
        // Current implementation returns empty list (placeholder)
        var questAnalytics = await _engine.GetQuestAnalyticsAsync();

        questAnalytics.Should().BeEmpty();
    }

    [Fact]
    public async Task GetInsightsAsync_SinglePlayer_ShouldCalculateCorrectly()
    {
        await _store.SetPlayerXpAsync("user1", 500);
        await _store.SetPlayerLevelAsync("user1", 3);
        await _store.RecordActivityAsync("user1");

        var insights = await _engine.GetInsightsAsync();

        insights.TotalPlayers.Should().Be(1);
        insights.AverageXpPerPlayer.Should().Be(500);
        insights.AverageLevel.Should().Be(3);
        insights.DailyActiveUsers.Should().Be(1);
        insights.WeeklyActiveUsers.Should().Be(1);
    }
}
