using FluentAssertions;
using GameifyKit.Configuration;
using Xunit;
using GameifyKit.Events;
using GameifyKit.Rules;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameifyKit.Tests.Rules;

public class CooldownTests
{
    private readonly GameEventBus _eventBus;

    public CooldownTests()
    {
        _eventBus = new GameEventBus(NullLogger<GameEventBus>.Instance);
    }

    private RuleEngine CreateEngine(RulesConfig config)
    {
        return new RuleEngine(config, _eventBus, NullLogger<RuleEngine>.Instance);
    }

    [Fact]
    public async Task ValidateAsync_NoCooldownConfigured_ShouldAllow()
    {
        var config = new RulesConfig { MaxDailyXp = 10000, MaxActionsPerHour = 200 };
        var engine = CreateEngine(config);

        await engine.RecordActionAsync("user1", "quiz", 10);
        var result = await engine.ValidateAsync("user1", "quiz", 10);

        result.Allowed.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WithCooldown_FirstAction_ShouldAllow()
    {
        var config = new RulesConfig { MaxDailyXp = 10000, MaxActionsPerHour = 200 };
        config.Cooldown("quiz", TimeSpan.FromMinutes(5));
        var engine = CreateEngine(config);

        var result = await engine.ValidateAsync("user1", "quiz", 10);

        result.Allowed.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WithCooldown_ImmediateRepeat_ShouldDeny()
    {
        var config = new RulesConfig { MaxDailyXp = 10000, MaxActionsPerHour = 200 };
        config.Cooldown("quiz", TimeSpan.FromMinutes(5));
        var engine = CreateEngine(config);

        // First action: record it (this sets the cooldown timestamp)
        await engine.RecordActionAsync("user1", "quiz", 10);

        // Immediate second action should be denied (cooldown not expired)
        var result = await engine.ValidateAsync("user1", "quiz", 10);

        result.Allowed.Should().BeFalse();
        result.Reason.Should().Contain("Cooldown");
        result.Reason.Should().Contain("quiz");
    }

    [Fact]
    public async Task ValidateAsync_DifferentActions_ShouldHaveSeparateCooldowns()
    {
        var config = new RulesConfig { MaxDailyXp = 10000, MaxActionsPerHour = 200 };
        config.Cooldown("quiz", TimeSpan.FromMinutes(5));
        config.Cooldown("login", TimeSpan.FromMinutes(10));
        var engine = CreateEngine(config);

        await engine.RecordActionAsync("user1", "quiz", 10);

        // quiz should be on cooldown, login should not
        var quizResult = await engine.ValidateAsync("user1", "quiz", 10);
        var loginResult = await engine.ValidateAsync("user1", "login", 10);

        quizResult.Allowed.Should().BeFalse();
        loginResult.Allowed.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_DifferentUsers_ShouldHaveSeparateCooldowns()
    {
        var config = new RulesConfig { MaxDailyXp = 10000, MaxActionsPerHour = 200 };
        config.Cooldown("quiz", TimeSpan.FromMinutes(5));
        var engine = CreateEngine(config);

        await engine.RecordActionAsync("user1", "quiz", 10);

        var user1Result = await engine.ValidateAsync("user1", "quiz", 10);
        var user2Result = await engine.ValidateAsync("user2", "quiz", 10);

        user1Result.Allowed.Should().BeFalse();
        user2Result.Allowed.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_ActionWithoutCooldown_ShouldNotBlockOtherActions()
    {
        var config = new RulesConfig { MaxDailyXp = 10000, MaxActionsPerHour = 200 };
        config.Cooldown("quiz", TimeSpan.FromMinutes(5));
        // No cooldown for "submit"
        var engine = CreateEngine(config);

        await engine.RecordActionAsync("user1", "submit", 10);
        var result = await engine.ValidateAsync("user1", "submit", 10);

        result.Allowed.Should().BeTrue();
    }

    [Fact]
    public void CooldownState_IsReady_WhenJustCreated_ShouldBeFalse()
    {
        var state = new CooldownState
        {
            LastActionTime = DateTimeOffset.UtcNow,
            CooldownDuration = TimeSpan.FromMinutes(5)
        };

        state.IsReady.Should().BeFalse();
    }

    [Fact]
    public void CooldownState_IsReady_WhenExpired_ShouldBeTrue()
    {
        var state = new CooldownState
        {
            LastActionTime = DateTimeOffset.UtcNow.AddMinutes(-10),
            CooldownDuration = TimeSpan.FromMinutes(5)
        };

        state.IsReady.Should().BeTrue();
    }

    [Fact]
    public void CooldownState_TimeRemaining_ShouldBePositiveWhenActive()
    {
        var state = new CooldownState
        {
            LastActionTime = DateTimeOffset.UtcNow,
            CooldownDuration = TimeSpan.FromMinutes(5)
        };

        state.TimeRemaining.Should().BeGreaterThan(TimeSpan.Zero);
        state.TimeRemaining.Should().BeLessThanOrEqualTo(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void CooldownState_TimeRemaining_ShouldBeZeroWhenExpired()
    {
        var state = new CooldownState
        {
            LastActionTime = DateTimeOffset.UtcNow.AddMinutes(-10),
            CooldownDuration = TimeSpan.FromMinutes(5)
        };

        state.TimeRemaining.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void RulesConfig_Cooldown_ShouldAddCooldownEntry()
    {
        var config = new RulesConfig();
        config.Cooldown("quiz", TimeSpan.FromSeconds(30));

        config.Cooldowns.Should().ContainKey("quiz");
        config.Cooldowns["quiz"].Should().Be(TimeSpan.FromSeconds(30));
    }
}
