using FluentAssertions;
using GameifyKit.Configuration;
using Xunit;
using GameifyKit.Events;
using GameifyKit.Leaderboard;
using GameifyKit.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameifyKit.Tests.Leaderboard;

public class TierCalculationTests
{
    private readonly InMemoryGameStore _store;
    private readonly GameEventBus _eventBus;

    public TierCalculationTests()
    {
        _store = new InMemoryGameStore();
        _eventBus = new GameEventBus(NullLogger<GameEventBus>.Instance);
    }

    private LeaderboardEngine CreateEngine(TierDefinition[] tiers)
    {
        var config = new LeaderboardConfig
        {
            Periods = new[] { LeaderboardPeriod.Weekly },
            DefaultPeriod = LeaderboardPeriod.Weekly,
            Tiers = tiers
        };
        return new LeaderboardEngine(
            _store, _eventBus, config,
            NullLogger<LeaderboardEngine>.Instance);
    }

    [Fact]
    public async Task TopPlayer_ShouldBeInHighestTier()
    {
        var tiers = new[]
        {
            new TierDefinition("bronze", "Bronze", "B", 0.5),
            new TierDefinition("silver", "Silver", "S", 0.8),
            new TierDefinition("gold", "Gold", "G", 1.0)
        };
        var engine = CreateEngine(tiers);

        // 10 players, user9 is top
        for (int i = 0; i < 10; i++)
            await _store.UpdateLeaderboardAsync($"user{i}", LeaderboardPeriod.Weekly, (i + 1) * 100);

        var top = await engine.GetTopAsync(LeaderboardPeriod.Weekly, 10);

        // Rank 1 => percentile = 1.0 - (0/10) = 1.0 => should be Gold (maxPercentile 1.0)
        top[0].Tier.Should().Be("Gold");
    }

    [Fact]
    public async Task BottomPlayer_ShouldBeInLowestTier()
    {
        var tiers = new[]
        {
            new TierDefinition("bronze", "Bronze", "B", 0.5),
            new TierDefinition("silver", "Silver", "S", 0.8),
            new TierDefinition("gold", "Gold", "G", 1.0)
        };
        var engine = CreateEngine(tiers);

        for (int i = 0; i < 10; i++)
            await _store.UpdateLeaderboardAsync($"user{i}", LeaderboardPeriod.Weekly, (i + 1) * 100);

        var top = await engine.GetTopAsync(LeaderboardPeriod.Weekly, 10);

        // Last place (rank 10) => percentile = 1.0 - 9/10 = 0.1 => Bronze (maxPercentile 0.5)
        top[9].Tier.Should().Be("Bronze");
    }

    [Fact]
    public async Task MiddlePlayer_ShouldBeInMiddleTier()
    {
        var tiers = new[]
        {
            new TierDefinition("bronze", "Bronze", "B", 0.5),
            new TierDefinition("silver", "Silver", "S", 0.8),
            new TierDefinition("gold", "Gold", "G", 1.0)
        };
        var engine = CreateEngine(tiers);

        for (int i = 0; i < 10; i++)
            await _store.UpdateLeaderboardAsync($"user{i}", LeaderboardPeriod.Weekly, (i + 1) * 100);

        var top = await engine.GetTopAsync(LeaderboardPeriod.Weekly, 10);

        // Rank 4 of 10 => percentile = 1.0 - 3/10 = 0.7 => Silver (maxPercentile 0.8, after bronze at 0.5)
        top[3].Tier.Should().Be("Silver");
    }

    [Fact]
    public async Task NoTiersConfigured_ShouldReturnNullTier()
    {
        var engine = CreateEngine(Array.Empty<TierDefinition>());

        await _store.UpdateLeaderboardAsync("user1", LeaderboardPeriod.Weekly, 500);

        var top = await engine.GetTopAsync(LeaderboardPeriod.Weekly, 10);

        top[0].Tier.Should().BeNull();
    }

    [Fact]
    public async Task SinglePlayer_ShouldBeInHighestTier()
    {
        var tiers = new[]
        {
            new TierDefinition("bronze", "Bronze", "B", 0.5),
            new TierDefinition("gold", "Gold", "G", 1.0)
        };
        var engine = CreateEngine(tiers);

        await _store.UpdateLeaderboardAsync("user1", LeaderboardPeriod.Weekly, 500);

        var top = await engine.GetTopAsync(LeaderboardPeriod.Weekly, 10);

        // Rank 1 of 1 => percentile = 1.0 => Gold
        top[0].Tier.Should().Be("Gold");
    }

    [Fact]
    public async Task Standing_ShouldIncludeCorrectTier()
    {
        var tiers = new[]
        {
            new TierDefinition("bronze", "Bronze", "B", 0.5),
            new TierDefinition("gold", "Gold", "G", 1.0)
        };
        var engine = CreateEngine(tiers);

        for (int i = 0; i < 10; i++)
            await _store.UpdateLeaderboardAsync($"user{i}", LeaderboardPeriod.Weekly, (i + 1) * 100);

        // user9 has highest XP (1000), rank 1 => gold
        var standing = await engine.GetStandingAsync("user9", LeaderboardPeriod.Weekly);

        standing.Tier.Should().Be("Gold");
        standing.TierIcon.Should().Be("G");
    }

    [Fact]
    public async Task TierAssignment_ShouldBeConsistentAcrossGetTopAndGetStanding()
    {
        var tiers = new[]
        {
            new TierDefinition("bronze", "Bronze", "B", 0.5),
            new TierDefinition("gold", "Gold", "G", 1.0)
        };
        var engine = CreateEngine(tiers);

        for (int i = 0; i < 10; i++)
            await _store.UpdateLeaderboardAsync($"user{i}", LeaderboardPeriod.Weekly, (i + 1) * 100);

        var top = await engine.GetTopAsync(LeaderboardPeriod.Weekly, 10);
        var standing = await engine.GetStandingAsync("user9", LeaderboardPeriod.Weekly);

        top[0].Tier.Should().Be(standing.Tier);
    }
}
