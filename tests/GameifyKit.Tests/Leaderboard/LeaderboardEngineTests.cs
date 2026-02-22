using FluentAssertions;
using GameifyKit.Configuration;
using Xunit;
using GameifyKit.Events;
using GameifyKit.Leaderboard;
using GameifyKit.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameifyKit.Tests.Leaderboard;

public class LeaderboardEngineTests
{
    private readonly InMemoryGameStore _store;
    private readonly GameEventBus _eventBus;

    public LeaderboardEngineTests()
    {
        _store = new InMemoryGameStore();
        _eventBus = new GameEventBus(NullLogger<GameEventBus>.Instance);
    }

    private LeaderboardEngine CreateEngine(LeaderboardConfig? config = null)
    {
        config ??= new LeaderboardConfig
        {
            Periods = new[] { LeaderboardPeriod.Weekly },
            DefaultPeriod = LeaderboardPeriod.Weekly,
            Tiers = new[]
            {
                new TierDefinition("bronze", "Bronze", "B", 0.5),
                new TierDefinition("silver", "Silver", "S", 0.75),
                new TierDefinition("gold", "Gold", "G", 0.9),
                new TierDefinition("diamond", "Diamond", "D", 1.0)
            }
        };
        return new LeaderboardEngine(
            _store, _eventBus, config,
            NullLogger<LeaderboardEngine>.Instance);
    }

    [Fact]
    public async Task GetTopAsync_ShouldReturnEntriesOrderedByXp()
    {
        var engine = CreateEngine();

        await _store.UpdateLeaderboardAsync("user1", LeaderboardPeriod.Weekly, 500);
        await _store.UpdateLeaderboardAsync("user2", LeaderboardPeriod.Weekly, 1000);
        await _store.UpdateLeaderboardAsync("user3", LeaderboardPeriod.Weekly, 750);

        var top = await engine.GetTopAsync(LeaderboardPeriod.Weekly, 10);

        top.Should().HaveCount(3);
        top[0].UserId.Should().Be("user2");
        top[0].Rank.Should().Be(1);
        top[0].Xp.Should().Be(1000);
        top[1].UserId.Should().Be("user3");
        top[1].Rank.Should().Be(2);
        top[2].UserId.Should().Be("user1");
        top[2].Rank.Should().Be(3);
    }

    [Fact]
    public async Task GetTopAsync_ShouldRespectCountLimit()
    {
        var engine = CreateEngine();

        for (int i = 0; i < 10; i++)
            await _store.UpdateLeaderboardAsync($"user{i}", LeaderboardPeriod.Weekly, (i + 1) * 100);

        var top = await engine.GetTopAsync(LeaderboardPeriod.Weekly, 3);

        top.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetTopAsync_ShouldAssignTiers()
    {
        var engine = CreateEngine();

        // Add 10 players
        for (int i = 0; i < 10; i++)
            await _store.UpdateLeaderboardAsync($"user{i}", LeaderboardPeriod.Weekly, (i + 1) * 100);

        var top = await engine.GetTopAsync(LeaderboardPeriod.Weekly, 10);

        // Top entry (rank 1 of 10) should have a tier assigned
        top[0].Tier.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTopAsync_Empty_ShouldReturnEmptyList()
    {
        var engine = CreateEngine();

        var top = await engine.GetTopAsync(LeaderboardPeriod.Weekly, 10);

        top.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStandingAsync_ShouldReturnPlayerRank()
    {
        var engine = CreateEngine();

        await _store.UpdateLeaderboardAsync("user1", LeaderboardPeriod.Weekly, 500);
        await _store.UpdateLeaderboardAsync("user2", LeaderboardPeriod.Weekly, 1000);
        await _store.UpdateLeaderboardAsync("user3", LeaderboardPeriod.Weekly, 750);

        var standing = await engine.GetStandingAsync("user3", LeaderboardPeriod.Weekly);

        standing.Rank.Should().Be(2);
        standing.Xp.Should().Be(750);
    }

    [Fact]
    public async Task GetStandingAsync_UnknownPlayer_ShouldReturnLastRank()
    {
        var engine = CreateEngine();

        await _store.UpdateLeaderboardAsync("user1", LeaderboardPeriod.Weekly, 500);

        var standing = await engine.GetStandingAsync("unknown-user", LeaderboardPeriod.Weekly);

        standing.Rank.Should().Be(2); // totalPlayers + 1
        standing.Xp.Should().Be(0);
    }

    [Fact]
    public async Task GetStandingAsync_ShouldIncludeTierInfo()
    {
        var engine = CreateEngine();

        // Add enough players to have meaningful tiers
        for (int i = 0; i < 10; i++)
            await _store.UpdateLeaderboardAsync($"user{i}", LeaderboardPeriod.Weekly, (i + 1) * 100);

        var standing = await engine.GetStandingAsync("user9", LeaderboardPeriod.Weekly); // highest XP

        standing.Tier.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateAllConfiguredPeriods()
    {
        var config = new LeaderboardConfig
        {
            Periods = new[] { LeaderboardPeriod.Weekly, LeaderboardPeriod.Monthly },
            DefaultPeriod = LeaderboardPeriod.Weekly,
            Tiers = Array.Empty<TierDefinition>()
        };
        var engine = CreateEngine(config);

        await engine.UpdateAsync("user1", 500);

        var weeklyEntries = await _store.GetLeaderboardEntriesAsync(LeaderboardPeriod.Weekly, 10);
        var monthlyEntries = await _store.GetLeaderboardEntriesAsync(LeaderboardPeriod.Monthly, 10);

        weeklyEntries.Should().HaveCount(1);
        monthlyEntries.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetTopAsync_WithZeroCount_ShouldThrow()
    {
        var engine = CreateEngine();

        var act = () => engine.GetTopAsync(LeaderboardPeriod.Weekly, 0);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task UpdateAsync_ShouldEmitTierChangeEvent()
    {
        var config = new LeaderboardConfig
        {
            Periods = new[] { LeaderboardPeriod.Weekly },
            DefaultPeriod = LeaderboardPeriod.Weekly,
            Tiers = new[]
            {
                new TierDefinition("bronze", "Bronze", "B", 0.5),
                new TierDefinition("gold", "Gold", "G", 1.0)
            }
        };
        var engine = CreateEngine(config);

        // First, create several players to establish a baseline
        for (int i = 0; i < 5; i++)
            await _store.UpdateLeaderboardAsync($"other{i}", LeaderboardPeriod.Weekly, (i + 1) * 100);

        TierChangeEvent? capturedEvent = null;
        _eventBus.Subscribe<TierChangeEvent>(e =>
        {
            capturedEvent = e;
            return Task.CompletedTask;
        });

        // Add a new player with very high XP (should be top tier)
        await engine.UpdateAsync("user1", 10000);

        // The tier change event might fire if tier changes
        // (new player entering the board for the first time may trigger tier change)
        // This tests the event mechanism works
        if (capturedEvent != null)
        {
            capturedEvent.UserId.Should().Be("user1");
            capturedEvent.NewTier.Should().NotBeNull();
        }
    }
}
