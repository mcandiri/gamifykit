using FluentAssertions;
using GameifyKit.Configuration;
using Xunit;
using GameifyKit.Events;
using GameifyKit.Rules;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameifyKit.Tests.Rules;

public class XpThrottleTests
{
    private readonly GameEventBus _eventBus;

    public XpThrottleTests()
    {
        _eventBus = new GameEventBus(NullLogger<GameEventBus>.Instance);
    }

    private RuleEngine CreateEngine(RulesConfig? config = null)
    {
        config ??= new RulesConfig
        {
            MaxDailyXp = 1000,
            MaxActionsPerHour = 200
        };
        return new RuleEngine(config, _eventBus, NullLogger<RuleEngine>.Instance);
    }

    [Fact]
    public async Task ValidateAsync_UnderDailyLimit_ShouldAllow()
    {
        var engine = CreateEngine();

        var result = await engine.ValidateAsync("user1", "quiz", 500);

        result.Allowed.Should().BeTrue();
        result.Reason.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_ExceedingDailyLimit_ShouldDeny()
    {
        var config = new RulesConfig { MaxDailyXp = 100, MaxActionsPerHour = 200 };
        var engine = CreateEngine(config);

        // Record some XP first
        await engine.RecordActionAsync("user1", "quiz", 80);

        // Now try to add 30 more (total would be 110 > 100)
        var result = await engine.ValidateAsync("user1", "quiz", 30);

        result.Allowed.Should().BeFalse();
        result.Reason.Should().Contain("Daily XP limit");
    }

    [Fact]
    public async Task ValidateAsync_ExactDailyLimit_ShouldAllow()
    {
        var config = new RulesConfig { MaxDailyXp = 100, MaxActionsPerHour = 200 };
        var engine = CreateEngine(config);

        await engine.RecordActionAsync("user1", "quiz", 50);

        var result = await engine.ValidateAsync("user1", "quiz", 50);

        result.Allowed.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_DailyLimitExceeded_ShouldEmitSuspiciousEvent()
    {
        var config = new RulesConfig { MaxDailyXp = 50, MaxActionsPerHour = 200 };
        var engine = CreateEngine(config);

        SuspiciousActivityEvent? capturedEvent = null;
        _eventBus.Subscribe<SuspiciousActivityEvent>(e =>
        {
            capturedEvent = e;
            return Task.CompletedTask;
        });

        await engine.RecordActionAsync("user1", "quiz", 40);
        await engine.ValidateAsync("user1", "quiz", 20);

        capturedEvent.Should().NotBeNull();
        capturedEvent!.Type.Should().Be("daily_xp_limit_exceeded");
        capturedEvent.UserId.Should().Be("user1");
    }

    [Fact]
    public async Task RecordActionAsync_ShouldTrackDailyXp()
    {
        var config = new RulesConfig { MaxDailyXp = 200, MaxActionsPerHour = 200 };
        var engine = CreateEngine(config);

        await engine.RecordActionAsync("user1", "quiz", 100);
        await engine.RecordActionAsync("user1", "quiz", 80);

        // Now trying to add 30 more should exceed
        var result = await engine.ValidateAsync("user1", "quiz", 30);

        result.Allowed.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_DifferentUsers_ShouldHaveSeparateLimits()
    {
        var config = new RulesConfig { MaxDailyXp = 100, MaxActionsPerHour = 200 };
        var engine = CreateEngine(config);

        await engine.RecordActionAsync("user1", "quiz", 90);
        await engine.RecordActionAsync("user2", "quiz", 50);

        var result1 = await engine.ValidateAsync("user1", "quiz", 20);
        var result2 = await engine.ValidateAsync("user2", "quiz", 20);

        result1.Allowed.Should().BeFalse(); // 90 + 20 = 110 > 100
        result2.Allowed.Should().BeTrue();  // 50 + 20 = 70 <= 100
    }

    [Fact]
    public async Task ValidateAsync_HourlyActionLimit_ShouldDenyWhenExceeded()
    {
        var config = new RulesConfig { MaxDailyXp = 999999, MaxActionsPerHour = 3 };
        var engine = CreateEngine(config);

        // Record 3 actions
        for (int i = 0; i < 3; i++)
            await engine.RecordActionAsync("user1", "quiz", 10);

        // 4th action should be denied
        var result = await engine.ValidateAsync("user1", "quiz", 10);

        result.Allowed.Should().BeFalse();
        result.Reason.Should().Contain("Hourly action limit");
    }

    [Fact]
    public async Task ValidateAsync_FirstActionEver_ShouldAllow()
    {
        var engine = CreateEngine();

        var result = await engine.ValidateAsync("new-user", "first-action", 10);

        result.Allowed.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_OnSuspiciousActivity_CallbackShouldBeInvoked()
    {
        string? callbackUserId = null;
        var config = new RulesConfig
        {
            MaxDailyXp = 50,
            MaxActionsPerHour = 200,
            OnSuspiciousActivity = (userId, evt) =>
            {
                callbackUserId = userId;
                return Task.CompletedTask;
            }
        };
        var engine = CreateEngine(config);

        await engine.RecordActionAsync("user1", "quiz", 40);
        await engine.ValidateAsync("user1", "quiz", 20);

        callbackUserId.Should().Be("user1");
    }
}
