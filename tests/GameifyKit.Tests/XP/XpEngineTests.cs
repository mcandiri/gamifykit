using FluentAssertions;
using GameifyKit.Boosts;
using Xunit;
using GameifyKit.Configuration;
using GameifyKit.Events;
using GameifyKit.Rules;
using GameifyKit.Storage;
using GameifyKit.XP;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GameifyKit.Tests.XP;

public class XpEngineTests
{
    private readonly InMemoryGameStore _store;
    private readonly GameEventBus _eventBus;
    private readonly Mock<IBoostEngine> _boostEngine;
    private readonly Mock<IRuleEngine> _ruleEngine;
    private readonly XpEngine _engine;

    public XpEngineTests()
    {
        _store = new InMemoryGameStore();
        _eventBus = new GameEventBus(NullLogger<GameEventBus>.Instance);
        _boostEngine = new Mock<IBoostEngine>();
        _ruleEngine = new Mock<IRuleEngine>();

        _boostEngine
            .Setup(b => b.GetMultiplierAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1.0);

        _ruleEngine
            .Setup(r => r.ValidateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RuleValidationResult.Allow());

        _ruleEngine
            .Setup(r => r.RecordActionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var config = new LevelingConfig
        {
            Curve = LevelCurve.Linear,
            BaseXp = 100,
            MaxLevel = 100
        };

        _engine = new XpEngine(
            _store, _eventBus, _boostEngine.Object, _ruleEngine.Object,
            config, NullLogger<XpEngine>.Instance);
    }

    [Fact]
    public async Task AddAsync_ShouldAddXpToPlayer()
    {
        var result = await _engine.AddAsync("user1", 50, "quiz");

        result.BaseXp.Should().Be(50);
        result.FinalXp.Should().Be(50);
        result.TotalXp.Should().Be(50);
        result.Throttled.Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_ShouldAccumulateXpOverMultipleCalls()
    {
        await _engine.AddAsync("user1", 50, "quiz");
        var result = await _engine.AddAsync("user1", 30, "quiz");

        result.TotalXp.Should().Be(80);
    }

    [Fact]
    public async Task AddAsync_ShouldApplyBoostMultiplier()
    {
        _boostEngine
            .Setup(b => b.GetMultiplierAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(2.0);

        var result = await _engine.AddAsync("user1", 50, "quiz");

        result.BaseXp.Should().Be(50);
        result.Multiplier.Should().Be(2.0);
        result.FinalXp.Should().Be(100);
        result.TotalXp.Should().Be(100);
    }

    [Fact]
    public async Task AddAsync_ShouldThrottleWhenRulesDeny()
    {
        _ruleEngine
            .Setup(r => r.ValidateAsync("user1", "quiz", 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(RuleValidationResult.Deny("Daily limit exceeded"));

        var result = await _engine.AddAsync("user1", 50, "quiz");

        result.Throttled.Should().BeTrue();
        result.FinalXp.Should().Be(0);
        result.TotalXp.Should().Be(0);
        result.LeveledUp.Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_ShouldDetectLevelUp()
    {
        // Linear curve: 100 XP per level. Need 100 XP to go from level 1 to level 2.
        var result = await _engine.AddAsync("user1", 100, "quiz");

        result.LeveledUp.Should().BeTrue();
        result.Level.Should().Be(2);
    }

    [Fact]
    public async Task AddAsync_ShouldNotLevelUpIfXpInsufficient()
    {
        var result = await _engine.AddAsync("user1", 50, "quiz");

        result.LeveledUp.Should().BeFalse();
        result.Level.Should().Be(1);
    }

    [Fact]
    public async Task AddAsync_ShouldEmitLevelUpEvent()
    {
        LevelUpEvent? capturedEvent = null;
        _eventBus.Subscribe<LevelUpEvent>(e =>
        {
            capturedEvent = e;
            return Task.CompletedTask;
        });

        await _engine.AddAsync("user1", 100, "quiz");

        capturedEvent.Should().NotBeNull();
        capturedEvent!.UserId.Should().Be("user1");
        capturedEvent.PreviousLevel.Should().Be(1);
        capturedEvent.NewLevel.Should().Be(2);
    }

    [Fact]
    public async Task AddAsync_ShouldEmitMultipleLevelUpEvents()
    {
        var events = new List<LevelUpEvent>();
        _eventBus.Subscribe<LevelUpEvent>(e =>
        {
            events.Add(e);
            return Task.CompletedTask;
        });

        // 300 XP with Linear 100 XP/level => jumps from level 1 to level 4 (levels 2, 3, 4)
        await _engine.AddAsync("user1", 300, "quiz");

        events.Should().HaveCount(3);
        events[0].NewLevel.Should().Be(2);
        events[1].NewLevel.Should().Be(3);
        events[2].NewLevel.Should().Be(4);
    }

    [Fact]
    public async Task AddAsync_ShouldRecordAction()
    {
        await _engine.AddAsync("user1", 50, "quiz");

        _ruleEngine.Verify(r => r.RecordActionAsync("user1", "quiz", 50, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_ShouldThrowOnNegativeAmount()
    {
        var act = () => _engine.AddAsync("user1", -10, "quiz");

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task AddAsync_ShouldThrowOnZeroAmount()
    {
        var act = () => _engine.AddAsync("user1", 0, "quiz");

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task GetTotalXpAsync_ShouldReturnStoredXp()
    {
        await _engine.AddAsync("user1", 75, "quiz");

        var totalXp = await _engine.GetTotalXpAsync("user1");

        totalXp.Should().Be(75);
    }

    [Fact]
    public async Task GetLevelAsync_ShouldReturnCorrectLevel()
    {
        await _engine.AddAsync("user1", 250, "quiz");

        var level = await _engine.GetLevelAsync("user1");

        level.Should().Be(3); // 100 XP per level, 250 XP => level 3
    }

    [Fact]
    public async Task AddAsync_ShouldPersistLevelInStore()
    {
        await _engine.AddAsync("user1", 100, "quiz");

        var storedLevel = await _store.GetPlayerLevelAsync("user1");

        storedLevel.Should().Be(2);
    }

    [Fact]
    public async Task AddAsync_WhenThrottled_ShouldNotRecordAction()
    {
        _ruleEngine
            .Setup(r => r.ValidateAsync("user1", "quiz", 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(RuleValidationResult.Deny("Throttled"));

        await _engine.AddAsync("user1", 50, "quiz");

        _ruleEngine.Verify(r => r.RecordActionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
