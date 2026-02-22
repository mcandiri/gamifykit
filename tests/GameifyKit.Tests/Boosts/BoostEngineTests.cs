using FluentAssertions;
using GameifyKit.Boosts;
using Xunit;
using GameifyKit.Configuration;
using GameifyKit.Events;
using GameifyKit.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameifyKit.Tests.Boosts;

public class BoostEngineTests
{
    private readonly InMemoryGameStore _store;
    private readonly GameEventBus _eventBus;

    public BoostEngineTests()
    {
        _store = new InMemoryGameStore();
        _eventBus = new GameEventBus(NullLogger<GameEventBus>.Instance);
    }

    private BoostEngine CreateEngine(BoostsConfig? config = null)
    {
        config ??= new BoostsConfig
        {
            MaxStackableBoosts = 3,
            MaxMultiplier = 5.0
        };
        return new BoostEngine(
            _store, _eventBus, config,
            NullLogger<BoostEngine>.Instance);
    }

    [Fact]
    public async Task ActivateAsync_ShouldAddBoost()
    {
        var engine = CreateEngine();
        var boost = new XpBoost
        {
            Multiplier = 2.0,
            Duration = TimeSpan.FromHours(1),
            Reason = "test"
        };

        await engine.ActivateAsync("user1", boost);

        var active = await engine.GetActiveBoostsAsync("user1");
        active.Should().HaveCount(1);
        active[0].Multiplier.Should().Be(2.0);
    }

    [Fact]
    public async Task ActivateAsync_ShouldEmitBoostActivatedEvent()
    {
        var engine = CreateEngine();
        var boost = new XpBoost
        {
            Multiplier = 2.0,
            Duration = TimeSpan.FromHours(1),
            Reason = "test"
        };

        BoostActivatedEvent? capturedEvent = null;
        _eventBus.Subscribe<BoostActivatedEvent>(e =>
        {
            capturedEvent = e;
            return Task.CompletedTask;
        });

        await engine.ActivateAsync("user1", boost);

        capturedEvent.Should().NotBeNull();
        capturedEvent!.UserId.Should().Be("user1");
        capturedEvent.Boost.Multiplier.Should().Be(2.0);
    }

    [Fact]
    public async Task ActivateAsync_ExceedingMaxStack_ShouldThrow()
    {
        var config = new BoostsConfig { MaxStackableBoosts = 2, MaxMultiplier = 10.0 };
        var engine = CreateEngine(config);

        await engine.ActivateAsync("user1", new XpBoost { Multiplier = 1.5, Duration = TimeSpan.FromHours(1), Reason = "b1" });
        await engine.ActivateAsync("user1", new XpBoost { Multiplier = 1.5, Duration = TimeSpan.FromHours(1), Reason = "b2" });

        var act = () => engine.ActivateAsync("user1", new XpBoost { Multiplier = 1.5, Duration = TimeSpan.FromHours(1), Reason = "b3" });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Maximum stackable boosts*");
    }

    [Fact]
    public async Task GetMultiplierAsync_NoBoosts_ShouldReturn1()
    {
        var engine = CreateEngine();

        var multiplier = await engine.GetMultiplierAsync("user1");

        multiplier.Should().Be(1.0);
    }

    [Fact]
    public async Task GetMultiplierAsync_SingleBoost_ShouldReturnBoostMultiplier()
    {
        var engine = CreateEngine();
        await engine.ActivateAsync("user1", new XpBoost { Multiplier = 2.0, Duration = TimeSpan.FromHours(1), Reason = "test" });

        var multiplier = await engine.GetMultiplierAsync("user1");

        multiplier.Should().Be(2.0);
    }

    [Fact]
    public async Task GetMultiplierAsync_MultipleBoosts_ShouldStackMultiplicatively()
    {
        var engine = CreateEngine();
        await engine.ActivateAsync("user1", new XpBoost { Multiplier = 2.0, Duration = TimeSpan.FromHours(1), Reason = "b1" });
        await engine.ActivateAsync("user1", new XpBoost { Multiplier = 1.5, Duration = TimeSpan.FromHours(1), Reason = "b2" });

        var multiplier = await engine.GetMultiplierAsync("user1");

        multiplier.Should().Be(3.0); // 2.0 * 1.5 = 3.0
    }

    [Fact]
    public async Task GetMultiplierAsync_ShouldRespectMaxCap()
    {
        var config = new BoostsConfig { MaxStackableBoosts = 3, MaxMultiplier = 4.0 };
        var engine = CreateEngine(config);

        await engine.ActivateAsync("user1", new XpBoost { Multiplier = 2.0, Duration = TimeSpan.FromHours(1), Reason = "b1" });
        await engine.ActivateAsync("user1", new XpBoost { Multiplier = 2.0, Duration = TimeSpan.FromHours(1), Reason = "b2" });
        await engine.ActivateAsync("user1", new XpBoost { Multiplier = 2.0, Duration = TimeSpan.FromHours(1), Reason = "b3" });

        var multiplier = await engine.GetMultiplierAsync("user1");

        // 2 * 2 * 2 = 8, but max is 4
        multiplier.Should().Be(4.0);
    }

    [Fact]
    public async Task GetActiveBoostsAsync_ExpiredBoost_ShouldNotBeReturned()
    {
        var engine = CreateEngine();
        var expiredBoost = new XpBoost
        {
            Multiplier = 2.0,
            Duration = TimeSpan.FromMilliseconds(1),
            Reason = "expired"
        };
        // Set activation time in the past so it expires
        expiredBoost.ActivatedAt = DateTimeOffset.UtcNow.AddHours(-2);

        await _store.AddBoostAsync("user1", expiredBoost);

        var active = await engine.GetActiveBoostsAsync("user1");

        active.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMultiplierAsync_ExpiredBoosts_ShouldBeIgnored()
    {
        var engine = CreateEngine();
        var expiredBoost = new XpBoost
        {
            Multiplier = 3.0,
            Duration = TimeSpan.FromMilliseconds(1),
            Reason = "expired"
        };
        expiredBoost.ActivatedAt = DateTimeOffset.UtcNow.AddHours(-2);

        await _store.AddBoostAsync("user1", expiredBoost);

        var multiplier = await engine.GetMultiplierAsync("user1");

        multiplier.Should().Be(1.0);
    }

    [Fact]
    public async Task GetActiveBoostsAsync_NoBoosts_ShouldReturnEmpty()
    {
        var engine = CreateEngine();

        var active = await engine.GetActiveBoostsAsync("user1");

        active.Should().BeEmpty();
    }

    [Fact]
    public async Task ActivateAsync_ShouldSetActivatedAt()
    {
        var engine = CreateEngine();
        var boost = new XpBoost
        {
            Multiplier = 2.0,
            Duration = TimeSpan.FromHours(1),
            Reason = "test"
        };

        var before = DateTimeOffset.UtcNow;
        await engine.ActivateAsync("user1", boost);
        var after = DateTimeOffset.UtcNow;

        boost.ActivatedAt.Should().BeOnOrAfter(before);
        boost.ActivatedAt.Should().BeOnOrBefore(after);
    }
}
