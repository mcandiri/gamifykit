using GameifyKit.Configuration;
using GameifyKit.Events;
using GameifyKit.Storage;
using Microsoft.Extensions.Logging;

namespace GameifyKit.Economy;

/// <summary>
/// Default implementation of the economy engine.
/// </summary>
public sealed class EconomyEngine : IEconomyEngine
{
    private readonly IGameStore _store;
    private readonly IGameEventBus _eventBus;
    private readonly EconomyConfig _config;
    private readonly ILogger<EconomyEngine> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EconomyEngine"/> class.
    /// </summary>
    public EconomyEngine(
        IGameStore store,
        IGameEventBus eventBus,
        EconomyConfig config,
        ILogger<EconomyEngine> logger)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<WalletInfo> GetWalletAsync(string userId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var wallet = await _store.GetWalletAsync(userId, ct);
        return new WalletInfo
        {
            Balance = wallet.Balance,
            CurrencyName = _config.CurrencyName,
            Lifetime = wallet.LifetimeEarned
        };
    }

    /// <inheritdoc />
    public async Task<PurchaseResult> PurchaseAsync(string userId, string rewardId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(rewardId);

        var reward = _config.Rewards.FirstOrDefault(r => r.Id == rewardId);
        if (reward == null)
        {
            return new PurchaseResult
            {
                Success = false,
                Reason = $"Reward '{rewardId}' not found."
            };
        }

        var wallet = await _store.GetWalletAsync(userId, ct);

        if (wallet.Balance < reward.Cost)
        {
            return new PurchaseResult
            {
                Success = false,
                Reason = "Insufficient balance",
                RemainingBalance = wallet.Balance
            };
        }

        // Check one-time purchase
        if (reward.OneTimePurchase)
        {
            var purchased = await _store.HasPurchasedAsync(userId, rewardId, ct);
            if (purchased)
            {
                return new PurchaseResult
                {
                    Success = false,
                    Reason = "Already purchased (one-time only)",
                    RemainingBalance = wallet.Balance
                };
            }
        }

        // Check daily purchase limit
        if (reward.MaxPurchasesPerDay > 0)
        {
            var todayCount = await _store.GetDailyPurchaseCountAsync(userId, rewardId, ct);
            if (todayCount >= reward.MaxPurchasesPerDay)
            {
                return new PurchaseResult
                {
                    Success = false,
                    Reason = $"Daily purchase limit ({reward.MaxPurchasesPerDay}) reached",
                    RemainingBalance = wallet.Balance
                };
            }
        }

        // Process purchase
        var newBalance = wallet.Balance - reward.Cost;
        await _store.SetWalletBalanceAsync(userId, newBalance, ct);
        await _store.RecordPurchaseAsync(userId, rewardId, ct);

        await _eventBus.PublishAsync(new PurchaseEvent
        {
            UserId = userId,
            Reward = reward,
            AmountSpent = reward.Cost,
            RemainingBalance = newBalance
        }, ct);

        _logger.LogInformation("Player {UserId} purchased {RewardName} for {Cost} {Currency}",
            userId, reward.Name, reward.Cost, _config.CurrencyName);

        return new PurchaseResult
        {
            Success = true,
            RemainingBalance = newBalance,
            Reward = reward
        };
    }

    /// <inheritdoc />
    public async Task AwardCurrencyAsync(string userId, long amount, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        if (amount <= 0) return;

        var wallet = await _store.GetWalletAsync(userId, ct);
        await _store.SetWalletBalanceAsync(userId, wallet.Balance + amount, ct);
        await _store.AddLifetimeEarnedAsync(userId, amount, ct);

        _logger.LogDebug("Awarded {Amount} {Currency} to {UserId}", amount, _config.CurrencyName, userId);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<RewardDefinition>> GetRewardsAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<RewardDefinition>>(_config.Rewards.AsReadOnly());
    }
}
