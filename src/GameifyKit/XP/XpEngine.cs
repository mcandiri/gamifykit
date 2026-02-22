using GameifyKit.Boosts;
using GameifyKit.Configuration;
using GameifyKit.Events;
using GameifyKit.Rules;
using GameifyKit.Storage;
using Microsoft.Extensions.Logging;

namespace GameifyKit.XP;

/// <summary>
/// Default implementation of the XP engine.
/// </summary>
public sealed class XpEngine : IXpEngine
{
    private readonly IGameStore _store;
    private readonly IGameEventBus _eventBus;
    private readonly IBoostEngine _boostEngine;
    private readonly IRuleEngine _ruleEngine;
    private readonly LevelCalculator _levelCalculator;
    private readonly ILogger<XpEngine> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="XpEngine"/> class.
    /// </summary>
    public XpEngine(
        IGameStore store,
        IGameEventBus eventBus,
        IBoostEngine boostEngine,
        IRuleEngine ruleEngine,
        LevelingConfig levelingConfig,
        ILogger<XpEngine> logger)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _boostEngine = boostEngine ?? throw new ArgumentNullException(nameof(boostEngine));
        _ruleEngine = ruleEngine ?? throw new ArgumentNullException(nameof(ruleEngine));
        _levelCalculator = new LevelCalculator(levelingConfig);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<XpResult> AddAsync(string userId, int amount, string action, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(action);

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "XP amount must be positive.");

        // Check rules
        var ruleResult = await _ruleEngine.ValidateAsync(userId, action, amount, ct);
        if (!ruleResult.Allowed)
        {
            _logger.LogWarning("XP award throttled for {UserId}: {Reason}", userId, ruleResult.Reason);
            var currentXp = await _store.GetPlayerXpAsync(userId, ct);
            return new XpResult
            {
                BaseXp = amount,
                Multiplier = 1.0,
                FinalXp = 0,
                TotalXp = currentXp,
                Level = _levelCalculator.GetLevel(currentXp),
                LeveledUp = false,
                Throttled = true
            };
        }

        // Get boost multiplier
        var multiplier = await _boostEngine.GetMultiplierAsync(userId, ct);

        var finalXp = (int)(amount * multiplier);
        var previousXp = await _store.GetPlayerXpAsync(userId, ct);
        var previousLevel = _levelCalculator.GetLevel(previousXp);

        var newTotalXp = previousXp + finalXp;
        await _store.SetPlayerXpAsync(userId, newTotalXp, ct);
        await _store.RecordActivityAsync(userId, ct);

        var newLevel = _levelCalculator.GetLevel(newTotalXp);
        if (newLevel != previousLevel)
        {
            await _store.SetPlayerLevelAsync(userId, newLevel, ct);
        }

        // Record action for rules
        await _ruleEngine.RecordActionAsync(userId, action, finalXp, ct);

        // Emit level-up event if applicable
        if (newLevel > previousLevel)
        {
            for (int lvl = previousLevel + 1; lvl <= newLevel; lvl++)
            {
                await _eventBus.PublishAsync(new LevelUpEvent
                {
                    UserId = userId,
                    PreviousLevel = lvl - 1,
                    NewLevel = lvl,
                    TotalXp = newTotalXp
                }, ct);
            }
        }

        _logger.LogDebug("Awarded {FinalXp} XP to {UserId} (base: {BaseXp}, multiplier: {Multiplier}x)",
            finalXp, userId, amount, multiplier);

        return new XpResult
        {
            BaseXp = amount,
            Multiplier = multiplier,
            FinalXp = finalXp,
            TotalXp = newTotalXp,
            Level = newLevel,
            LeveledUp = newLevel > previousLevel,
            Throttled = false
        };
    }

    /// <inheritdoc />
    public async Task<long> GetTotalXpAsync(string userId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        return await _store.GetPlayerXpAsync(userId, ct);
    }

    /// <inheritdoc />
    public async Task<int> GetLevelAsync(string userId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        var xp = await _store.GetPlayerXpAsync(userId, ct);
        return _levelCalculator.GetLevel(xp);
    }
}
