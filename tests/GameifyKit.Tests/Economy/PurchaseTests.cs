using FluentAssertions;
using GameifyKit.Configuration;
using Xunit;
using GameifyKit.Economy;
using GameifyKit.Events;
using GameifyKit.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameifyKit.Tests.Economy;

public class PurchaseTests
{
    private readonly InMemoryGameStore _store;
    private readonly GameEventBus _eventBus;

    public PurchaseTests()
    {
        _store = new InMemoryGameStore();
        _eventBus = new GameEventBus(NullLogger<GameEventBus>.Instance);
    }

    private EconomyEngine CreateEngine(EconomyConfig? config = null)
    {
        config ??= CreateDefaultConfig();
        return new EconomyEngine(
            _store, _eventBus, config,
            NullLogger<EconomyEngine>.Instance);
    }

    private static EconomyConfig CreateDefaultConfig()
    {
        var config = new EconomyConfig { CurrencyName = "coins" };
        config.DefineReward("sword", r =>
        {
            r.Name = "Magic Sword";
            r.Cost = 100;
        });
        config.DefineReward("shield", r =>
        {
            r.Name = "Iron Shield";
            r.Cost = 50;
        });
        return config;
    }

    [Fact]
    public async Task PurchaseAsync_WithSufficientBalance_ShouldSucceed()
    {
        var engine = CreateEngine();
        await engine.AwardCurrencyAsync("user1", 200);

        var result = await engine.PurchaseAsync("user1", "sword");

        result.Success.Should().BeTrue();
        result.RemainingBalance.Should().Be(100); // 200 - 100
        result.Reward.Should().NotBeNull();
        result.Reward!.Id.Should().Be("sword");
    }

    [Fact]
    public async Task PurchaseAsync_WithInsufficientBalance_ShouldFail()
    {
        var engine = CreateEngine();
        await engine.AwardCurrencyAsync("user1", 50);

        var result = await engine.PurchaseAsync("user1", "sword");

        result.Success.Should().BeFalse();
        result.Reason.Should().Contain("Insufficient");
        result.RemainingBalance.Should().Be(50);
    }

    [Fact]
    public async Task PurchaseAsync_UnknownReward_ShouldFail()
    {
        var engine = CreateEngine();

        var result = await engine.PurchaseAsync("user1", "unknown-reward");

        result.Success.Should().BeFalse();
        result.Reason.Should().Contain("not found");
    }

    [Fact]
    public async Task PurchaseAsync_ShouldDeductBalance()
    {
        var engine = CreateEngine();
        await engine.AwardCurrencyAsync("user1", 200);

        await engine.PurchaseAsync("user1", "sword");

        var wallet = await engine.GetWalletAsync("user1");
        wallet.Balance.Should().Be(100);
    }

    [Fact]
    public async Task PurchaseAsync_ShouldEmitPurchaseEvent()
    {
        var engine = CreateEngine();
        await engine.AwardCurrencyAsync("user1", 200);

        PurchaseEvent? capturedEvent = null;
        _eventBus.Subscribe<PurchaseEvent>(e =>
        {
            capturedEvent = e;
            return Task.CompletedTask;
        });

        await engine.PurchaseAsync("user1", "sword");

        capturedEvent.Should().NotBeNull();
        capturedEvent!.UserId.Should().Be("user1");
        capturedEvent.Reward.Id.Should().Be("sword");
        capturedEvent.AmountSpent.Should().Be(100);
        capturedEvent.RemainingBalance.Should().Be(100);
    }

    [Fact]
    public async Task PurchaseAsync_OneTimePurchase_ShouldPreventRepurchase()
    {
        var config = new EconomyConfig();
        config.DefineReward("unique-item", r =>
        {
            r.Name = "Unique Item";
            r.Cost = 50;
            r.OneTimePurchase = true;
        });
        var engine = CreateEngine(config);
        await engine.AwardCurrencyAsync("user1", 200);

        var first = await engine.PurchaseAsync("user1", "unique-item");
        var second = await engine.PurchaseAsync("user1", "unique-item");

        first.Success.Should().BeTrue();
        second.Success.Should().BeFalse();
        second.Reason.Should().Contain("one-time");
    }

    [Fact]
    public async Task PurchaseAsync_DailyLimit_ShouldEnforceLimitPerDay()
    {
        var config = new EconomyConfig();
        config.DefineReward("potion", r =>
        {
            r.Name = "Health Potion";
            r.Cost = 10;
            r.MaxPurchasesPerDay = 2;
        });
        var engine = CreateEngine(config);
        await engine.AwardCurrencyAsync("user1", 1000);

        var first = await engine.PurchaseAsync("user1", "potion");
        var second = await engine.PurchaseAsync("user1", "potion");
        var third = await engine.PurchaseAsync("user1", "potion");

        first.Success.Should().BeTrue();
        second.Success.Should().BeTrue();
        third.Success.Should().BeFalse();
        third.Reason.Should().Contain("Daily purchase limit");
    }

    [Fact]
    public async Task PurchaseAsync_ExactBalance_ShouldSucceed()
    {
        var engine = CreateEngine();
        await engine.AwardCurrencyAsync("user1", 100);

        var result = await engine.PurchaseAsync("user1", "sword");

        result.Success.Should().BeTrue();
        result.RemainingBalance.Should().Be(0);
    }

    [Fact]
    public async Task PurchaseAsync_MultiplePurchases_ShouldDeductCorrectly()
    {
        var engine = CreateEngine();
        await engine.AwardCurrencyAsync("user1", 200);

        await engine.PurchaseAsync("user1", "sword"); // -100
        await engine.PurchaseAsync("user1", "shield"); // -50

        var wallet = await engine.GetWalletAsync("user1");
        wallet.Balance.Should().Be(50);
    }

    [Fact]
    public async Task PurchaseAsync_ZeroBalance_ShouldFail()
    {
        var engine = CreateEngine();

        var result = await engine.PurchaseAsync("user1", "sword");

        result.Success.Should().BeFalse();
        result.Reason.Should().Contain("Insufficient");
    }

    [Fact]
    public async Task PurchaseAsync_WithInsufficientBalance_ShouldNotDeductBalance()
    {
        var engine = CreateEngine();
        await engine.AwardCurrencyAsync("user1", 50);

        await engine.PurchaseAsync("user1", "sword"); // costs 100, have 50

        var wallet = await engine.GetWalletAsync("user1");
        wallet.Balance.Should().Be(50); // Balance unchanged
    }
}
