using GameifyKit.Configuration;
using GameifyKit.Events;
using GameifyKit.Storage;
using Microsoft.Extensions.Logging;

namespace GameifyKit.Leaderboard;

/// <summary>
/// Default implementation of the leaderboard engine.
/// </summary>
public sealed class LeaderboardEngine : ILeaderboardEngine
{
    private readonly IGameStore _store;
    private readonly IGameEventBus _eventBus;
    private readonly LeaderboardConfig _config;
    private readonly ILogger<LeaderboardEngine> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LeaderboardEngine"/> class.
    /// </summary>
    public LeaderboardEngine(
        IGameStore store,
        IGameEventBus eventBus,
        LeaderboardConfig config,
        ILogger<LeaderboardEngine> logger)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LeaderboardEntry>> GetTopAsync(LeaderboardPeriod period, int count = 10, CancellationToken ct = default)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive.");

        var entries = await _store.GetLeaderboardEntriesAsync(period, count, ct);
        var result = new List<LeaderboardEntry>();
        int rank = 1;

        foreach (var entry in entries.OrderByDescending(e => e.Xp))
        {
            var tier = CalculateTier(rank, entries.Count);
            result.Add(new LeaderboardEntry
            {
                Rank = rank,
                UserId = entry.UserId,
                DisplayName = entry.DisplayName,
                Xp = entry.Xp,
                Level = entry.Level,
                Tier = tier?.Name
            });
            rank++;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<PlayerStanding> GetStandingAsync(string userId, LeaderboardPeriod period, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var allEntries = await _store.GetLeaderboardEntriesAsync(period, int.MaxValue, ct);
        var sorted = allEntries.OrderByDescending(e => e.Xp).ToList();
        var totalPlayers = sorted.Count;

        var playerIndex = sorted.FindIndex(e => e.UserId == userId);
        if (playerIndex < 0)
        {
            return new PlayerStanding
            {
                Rank = totalPlayers + 1,
                Xp = 0,
                Tier = _config.Tiers.FirstOrDefault()?.Name,
                TierIcon = _config.Tiers.FirstOrDefault()?.Icon
            };
        }

        int rank = playerIndex + 1;
        var playerEntry = sorted[playerIndex];
        var currentTier = CalculateTier(rank, totalPlayers);
        var nextTier = GetNextTier(currentTier);

        long pointsToNext = 0;
        if (nextTier != null && totalPlayers > 0)
        {
            int nextTierMinRank = (int)((1.0 - nextTier.MaxPercentile) * totalPlayers) + 1;
            if (nextTierMinRank > 0 && nextTierMinRank <= sorted.Count)
            {
                pointsToNext = sorted[nextTierMinRank - 1].Xp - playerEntry.Xp;
                if (pointsToNext < 0) pointsToNext = 0;
            }
        }

        return new PlayerStanding
        {
            Rank = rank,
            Tier = currentTier?.Name,
            TierIcon = currentTier?.Icon,
            Xp = playerEntry.Xp,
            PointsToNextTier = pointsToNext,
            AtRiskOfDemotion = false
        };
    }

    /// <inheritdoc />
    public async Task UpdateAsync(string userId, long xp, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var previousEntries = await _store.GetLeaderboardEntriesAsync(_config.DefaultPeriod, int.MaxValue, ct);
        var previousSorted = previousEntries.OrderByDescending(e => e.Xp).ToList();
        var previousIndex = previousSorted.FindIndex(e => e.UserId == userId);
        var previousTier = previousIndex >= 0 ? CalculateTier(previousIndex + 1, previousSorted.Count) : null;

        foreach (var period in _config.Periods)
        {
            await _store.UpdateLeaderboardAsync(userId, period, xp, ct);
        }

        // Check for tier change
        var newEntries = await _store.GetLeaderboardEntriesAsync(_config.DefaultPeriod, int.MaxValue, ct);
        var newSorted = newEntries.OrderByDescending(e => e.Xp).ToList();
        var newIndex = newSorted.FindIndex(e => e.UserId == userId);
        var newTier = newIndex >= 0 ? CalculateTier(newIndex + 1, newSorted.Count) : null;

        if (newTier != null && previousTier?.Id != newTier.Id)
        {
            var direction = GetTierRank(newTier) > GetTierRank(previousTier)
                ? TierDirection.Promoted
                : TierDirection.Demoted;

            await _eventBus.PublishAsync(new TierChangeEvent
            {
                UserId = userId,
                PreviousTier = previousTier,
                NewTier = newTier,
                Direction = direction
            }, ct);

            _logger.LogInformation("Player {UserId} tier changed: {PreviousTier} -> {NewTier}",
                userId, previousTier?.Name, newTier.Name);
        }
    }

    private TierDefinition? CalculateTier(int rank, int totalPlayers)
    {
        if (_config.Tiers.Length == 0 || totalPlayers == 0) return null;

        double percentile = 1.0 - ((double)(rank - 1) / totalPlayers);

        // Iterate ascending by maxPercentile — first match is the correct tier
        foreach (var tier in _config.Tiers.OrderBy(t => t.MaxPercentile))
        {
            if (percentile <= tier.MaxPercentile)
                return tier;
        }

        return _config.Tiers.OrderByDescending(t => t.MaxPercentile).First();
    }

    private TierDefinition? GetNextTier(TierDefinition? currentTier)
    {
        if (currentTier == null || _config.Tiers.Length == 0) return null;

        var sorted = _config.Tiers.OrderBy(t => t.MaxPercentile).ToList();
        var index = sorted.FindIndex(t => t.Id == currentTier.Id);
        return index >= 0 && index < sorted.Count - 1 ? sorted[index + 1] : null;
    }

    private int GetTierRank(TierDefinition? tier)
    {
        if (tier == null) return 0;
        var sorted = _config.Tiers.OrderBy(t => t.MaxPercentile).ToList();
        return sorted.FindIndex(t => t.Id == tier.Id) + 1;
    }
}
