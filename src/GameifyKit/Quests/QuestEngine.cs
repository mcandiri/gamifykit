using GameifyKit.Events;
using GameifyKit.Storage;
using Microsoft.Extensions.Logging;

namespace GameifyKit.Quests;

/// <summary>
/// Default implementation of the quest engine.
/// </summary>
public sealed class QuestEngine : IQuestEngine
{
    private readonly IGameStore _store;
    private readonly IGameEventBus _eventBus;
    private readonly IReadOnlyList<QuestDefinition> _definitions;
    private readonly ILogger<QuestEngine> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="QuestEngine"/> class.
    /// </summary>
    public QuestEngine(
        IGameStore store,
        IGameEventBus eventBus,
        IReadOnlyList<QuestDefinition> definitions,
        ILogger<QuestEngine> logger)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task AssignAsync(string userId, string questId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(questId);

        var quest = _definitions.FirstOrDefault(q => q.Id == questId)
            ?? throw new ArgumentException($"Quest '{questId}' not found.", nameof(questId));

        await _store.AssignQuestAsync(userId, questId, ct);
        _logger.LogDebug("Assigned quest {QuestId} to player {UserId}", questId, userId);
    }

    /// <inheritdoc />
    public async Task<QuestProgress> ProgressAsync(string userId, string questId, string stepId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(questId);
        ArgumentNullException.ThrowIfNull(stepId);

        var quest = _definitions.FirstOrDefault(q => q.Id == questId)
            ?? throw new ArgumentException($"Quest '{questId}' not found.", nameof(questId));

        var step = quest.Steps.FirstOrDefault(s => s.Id == stepId)
            ?? throw new ArgumentException($"Step '{stepId}' not found in quest '{questId}'.", nameof(stepId));

        await _store.CompleteQuestStepAsync(userId, questId, stepId, ct);

        var completedSteps = await _store.GetCompletedQuestStepsAsync(userId, questId, ct);
        var startedAt = await _store.GetQuestStartTimeAsync(userId, questId, ct);

        var progress = new QuestProgress
        {
            Quest = quest,
            CompletedSteps = completedSteps.ToHashSet(),
            StartedAt = startedAt
        };

        if (progress.IsCompleted)
        {
            var totalXp = quest.Steps.Sum(s => s.XpReward) + quest.CompletionBonus;
            await _eventBus.PublishAsync(new QuestCompletedEvent
            {
                UserId = userId,
                Quest = quest,
                TotalXpEarned = totalXp
            }, ct);

            _logger.LogInformation("Player {UserId} completed quest: {QuestName}", userId, quest.Name);
        }

        return progress;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<QuestProgress>> GetActiveAsync(string userId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var activeQuestIds = await _store.GetActiveQuestIdsAsync(userId, ct);
        var result = new List<QuestProgress>();

        foreach (var questId in activeQuestIds)
        {
            var quest = _definitions.FirstOrDefault(q => q.Id == questId);
            if (quest == null) continue;

            var completedSteps = await _store.GetCompletedQuestStepsAsync(userId, questId, ct);
            var startedAt = await _store.GetQuestStartTimeAsync(userId, questId, ct);

            result.Add(new QuestProgress
            {
                Quest = quest,
                CompletedSteps = completedSteps.ToHashSet(),
                StartedAt = startedAt
            });
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<QuestProgress?> GetProgressAsync(string userId, string questId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(questId);

        var quest = _definitions.FirstOrDefault(q => q.Id == questId);
        if (quest == null) return null;

        var activeQuestIds = await _store.GetActiveQuestIdsAsync(userId, ct);
        if (!activeQuestIds.Contains(questId)) return null;

        var completedSteps = await _store.GetCompletedQuestStepsAsync(userId, questId, ct);
        var startedAt = await _store.GetQuestStartTimeAsync(userId, questId, ct);

        return new QuestProgress
        {
            Quest = quest,
            CompletedSteps = completedSteps.ToHashSet(),
            StartedAt = startedAt
        };
    }
}

/// <summary>
/// Builder for configuring quests.
/// </summary>
public sealed class QuestBuilder
{
    private readonly List<QuestDefinition> _quests;

    /// <summary>
    /// Initializes a new instance of the <see cref="QuestBuilder"/> class.
    /// </summary>
    public QuestBuilder(List<QuestDefinition> quests)
    {
        _quests = quests;
    }

    /// <summary>
    /// Defines a new quest.
    /// </summary>
    public void Define(string id, Action<QuestDefinition> configure)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(configure);

        var quest = new QuestDefinition { Id = id };
        configure(quest);
        _quests.Add(quest);
    }
}
