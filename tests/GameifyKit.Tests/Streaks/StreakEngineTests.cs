using FluentAssertions;
using GameifyKit.Events;
using Xunit;
using GameifyKit.Storage;
using GameifyKit.Streaks;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameifyKit.Tests.Streaks;

public class StreakEngineTests
{
    private readonly InMemoryGameStore _store;
    private readonly GameEventBus _eventBus;

    public StreakEngineTests()
    {
        _store = new InMemoryGameStore();
        _eventBus = new GameEventBus(NullLogger<GameEventBus>.Instance);
    }

    private static StreakDefinition CreateDailyStreak(
        string id = "daily-login",
        TimeSpan? gracePeriod = null,
        StreakMilestone[]? milestones = null)
    {
        return new StreakDefinition
        {
            Id = id,
            Name = "Daily Login",
            Period = StreakPeriod.Daily,
            GracePeriod = gracePeriod ?? TimeSpan.FromHours(36),
            Milestones = milestones ?? Array.Empty<StreakMilestone>()
        };
    }

    private StreakEngine CreateEngine(params StreakDefinition[] definitions)
    {
        return new StreakEngine(
            _store, _eventBus, definitions.ToList(),
            NullLogger<StreakEngine>.Instance);
    }

    [Fact]
    public async Task RecordAsync_FirstTime_ShouldStartStreakAt1()
    {
        var engine = CreateEngine(CreateDailyStreak());

        var result = await engine.RecordAsync("user1", "daily-login");

        result.CurrentStreak.Should().Be(1);
        result.BestStreak.Should().Be(1);
        result.IsAlive.Should().BeTrue();
    }

    [Fact]
    public async Task RecordAsync_WithinSamePeriod_ShouldNotIncrement()
    {
        var engine = CreateEngine(CreateDailyStreak());

        var first = await engine.RecordAsync("user1", "daily-login");
        var second = await engine.RecordAsync("user1", "daily-login");

        // Second call within same day period should return same streak count
        second.CurrentStreak.Should().Be(1);
    }

    [Fact]
    public async Task RecordAsync_AfterOnePeriod_ShouldIncrementStreak()
    {
        var def = CreateDailyStreak();
        var engine = CreateEngine(def);

        // Record day 1
        await engine.RecordAsync("user1", "daily-login");

        // Simulate: manually set the last recorded to 25 hours ago (just past one period)
        var yesterday = DateTimeOffset.UtcNow.AddHours(-25);
        await _store.SetStreakDataAsync("user1", "daily-login", 1, 1, yesterday);

        var result = await engine.RecordAsync("user1", "daily-login");

        result.CurrentStreak.Should().Be(2);
        result.BestStreak.Should().Be(2);
    }

    [Fact]
    public async Task RecordAsync_BrokenStreak_ShouldResetTo1()
    {
        var def = CreateDailyStreak(gracePeriod: TimeSpan.FromHours(12));
        var engine = CreateEngine(def);

        // Simulate: last recorded was 3 days ago (well past period + grace)
        var threeDaysAgo = DateTimeOffset.UtcNow.AddDays(-3);
        await _store.SetStreakDataAsync("user1", "daily-login", 5, 5, threeDaysAgo);

        var result = await engine.RecordAsync("user1", "daily-login");

        result.CurrentStreak.Should().Be(1);
        result.BestStreak.Should().Be(5); // Best streak preserved
    }

    [Fact]
    public async Task RecordAsync_WithGracePeriod_ShouldContinueStreak()
    {
        var def = CreateDailyStreak(gracePeriod: TimeSpan.FromHours(36));
        var engine = CreateEngine(def);

        // Last recorded 30 hours ago (past 24h period, but within 24h + 36h = 60h grace window)
        var thirtyHoursAgo = DateTimeOffset.UtcNow.AddHours(-30);
        await _store.SetStreakDataAsync("user1", "daily-login", 3, 3, thirtyHoursAgo);

        var result = await engine.RecordAsync("user1", "daily-login");

        result.CurrentStreak.Should().Be(4);
        result.IsAlive.Should().BeTrue();
    }

    [Fact]
    public async Task RecordAsync_ShouldEmitMilestoneEvent()
    {
        var milestones = new[]
        {
            new StreakMilestone(3, 100, "3-day badge"),
            new StreakMilestone(7, 200, "7-day badge")
        };
        var def = CreateDailyStreak(milestones: milestones);
        var engine = CreateEngine(def);

        StreakMilestoneEvent? capturedEvent = null;
        _eventBus.Subscribe<StreakMilestoneEvent>(e =>
        {
            capturedEvent = e;
            return Task.CompletedTask;
        });

        // Set streak to 2 (just before the milestone of 3)
        var yesterday = DateTimeOffset.UtcNow.AddHours(-25);
        await _store.SetStreakDataAsync("user1", "daily-login", 2, 2, yesterday);

        var result = await engine.RecordAsync("user1", "daily-login");

        result.CurrentStreak.Should().Be(3);
        result.MilestoneReached.Should().Be("3-day badge");
        capturedEvent.Should().NotBeNull();
        capturedEvent!.Milestone.Badge.Should().Be("3-day badge");
        capturedEvent.CurrentStreak.Should().Be(3);
    }

    [Fact]
    public async Task RecordAsync_ShouldUpdateBestStreak()
    {
        var def = CreateDailyStreak();
        var engine = CreateEngine(def);

        // Simulate: current streak 10, best streak 10, recorded yesterday
        var yesterday = DateTimeOffset.UtcNow.AddHours(-25);
        await _store.SetStreakDataAsync("user1", "daily-login", 10, 10, yesterday);

        var result = await engine.RecordAsync("user1", "daily-login");

        result.CurrentStreak.Should().Be(11);
        result.BestStreak.Should().Be(11);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnStreakInfo()
    {
        var def = CreateDailyStreak();
        var engine = CreateEngine(def);

        await engine.RecordAsync("user1", "daily-login");

        var info = await engine.GetAsync("user1", "daily-login");

        info.Should().NotBeNull();
        info!.CurrentStreak.Should().Be(1);
        info.IsAlive.Should().BeTrue();
    }

    [Fact]
    public async Task GetAsync_UnknownStreak_ShouldReturnNull()
    {
        var engine = CreateEngine(CreateDailyStreak());

        var info = await engine.GetAsync("user1", "unknown-streak");

        info.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllStreaks()
    {
        var def1 = CreateDailyStreak("streak1");
        var def2 = CreateDailyStreak("streak2");
        def2.Name = "Streak 2";
        var engine = CreateEngine(def1, def2);

        await engine.RecordAsync("user1", "streak1");

        var all = await engine.GetAllAsync("user1");

        all.Should().HaveCount(2);
    }

    [Fact]
    public async Task RecordAsync_UnknownStreak_ShouldThrow()
    {
        var engine = CreateEngine(CreateDailyStreak());

        var act = () => engine.RecordAsync("user1", "unknown");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*unknown*");
    }

    [Fact]
    public async Task GetAsync_ExpiredStreak_ShouldShowNotAlive()
    {
        var def = CreateDailyStreak(gracePeriod: TimeSpan.FromHours(12));
        var engine = CreateEngine(def);

        // Set last recorded 3 days ago (well past grace)
        var threeDaysAgo = DateTimeOffset.UtcNow.AddDays(-3);
        await _store.SetStreakDataAsync("user1", "daily-login", 5, 5, threeDaysAgo);

        var info = await engine.GetAsync("user1", "daily-login");

        info.Should().NotBeNull();
        info!.IsAlive.Should().BeFalse();
        info.CurrentStreak.Should().Be(0); // Not alive means current streak is 0
        info.BestStreak.Should().Be(5);
    }
}
