using FluentAssertions;
using GameifyKit.Configuration;
using Xunit;
using GameifyKit.Economy;
using GameifyKit.Events;
using GameifyKit.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameifyKit.Tests.Economy;

public class EconomyEngineTests
{
    private readonly InMemoryGameStore _store;
    private readonly GameEventBus _eventBus;

    public EconomyEngineTests()
    {
        _store = new InMemoryGameStore();
        _eventBus = new GameEventBus(NullLogger<GameEventBus>.Instance);
    }

    private EconomyEngine CreateEngine(EconomyConfig? config = null)
    {
        config ??= new EconomyConfig
        {
            CurrencyName = "coins",
            XpToCurrencyRatio = 10
        };
        return new EconomyEngine(
            _store, _eventBus, config,
            NullLogger<EconomyEngine>.Instance);
    }

    [Fact]
    public async Task GetWalletAsync_NewPlayer_ShouldReturnZeroBalance()
    {
        var engine = CreateEngine();

        var wallet = await engine.GetWalletAsync("user1");

        wallet.Balance.Should().Be(0);
        wallet.CurrencyName.Should().Be("coins");
        wallet.Lifetime.Should().Be(0);
    }

    [Fact]
    public async Task AwardCurrencyAsync_ShouldIncreaseBalance()
    {
        var engine = CreateEngine();

        await engine.AwardCurrencyAsync("user1", 100);

        var wallet = await engine.GetWalletAsync("user1");
        wallet.Balance.Should().Be(100);
    }

    [Fact]
    public async Task AwardCurrencyAsync_ShouldTrackLifetimeEarned()
    {
        var engine = CreateEngine();

        await engine.AwardCurrencyAsync("user1", 100);
        await engine.AwardCurrencyAsync("user1", 50);

        var wallet = await engine.GetWalletAsync("user1");
        wallet.Balance.Should().Be(150);
        wallet.Lifetime.Should().Be(150);
    }

    [Fact]
    public async Task AwardCurrencyAsync_ZeroAmount_ShouldNotChangeBalance()
    {
        var engine = CreateEngine();

        await engine.AwardCurrencyAsync("user1", 0);

        var wallet = await engine.GetWalletAsync("user1");
        wallet.Balance.Should().Be(0);
    }

    [Fact]
    public async Task AwardCurrencyAsync_NegativeAmount_ShouldNotChangeBalance()
    {
        var engine = CreateEngine();

        await engine.AwardCurrencyAsync("user1", -50);

        var wallet = await engine.GetWalletAsync("user1");
        wallet.Balance.Should().Be(0);
    }

    [Fact]
    public async Task AwardCurrencyAsync_MultipleTimes_ShouldAccumulate()
    {
        var engine = CreateEngine();

        await engine.AwardCurrencyAsync("user1", 100);
        await engine.AwardCurrencyAsync("user1", 200);
        await engine.AwardCurrencyAsync("user1", 300);

        var wallet = await engine.GetWalletAsync("user1");
        wallet.Balance.Should().Be(600);
    }

    [Fact]
    public async Task GetRewardsAsync_ShouldReturnConfiguredRewards()
    {
        var config = new EconomyConfig();
        config.DefineReward("reward1", r =>
        {
            r.Name = "Test Reward";
            r.Cost = 100;
        });
        var engine = CreateEngine(config);

        var rewards = await engine.GetRewardsAsync();

        rewards.Should().HaveCount(1);
        rewards[0].Id.Should().Be("reward1");
        rewards[0].Name.Should().Be("Test Reward");
        rewards[0].Cost.Should().Be(100);
    }

    [Fact]
    public async Task GetWalletAsync_ShouldUseConfiguredCurrencyName()
    {
        var config = new EconomyConfig { CurrencyName = "gems" };
        var engine = CreateEngine(config);

        var wallet = await engine.GetWalletAsync("user1");

        wallet.CurrencyName.Should().Be("gems");
    }
}
