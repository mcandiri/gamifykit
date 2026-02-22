using FluentAssertions;
using GameifyKit.Events;
using Xunit;
using GameifyKit.Quests;
using GameifyKit.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameifyKit.Tests.Quests;

public class QuestEngineTests
{
    private readonly InMemoryGameStore _store;
    private readonly GameEventBus _eventBus;

    public QuestEngineTests()
    {
        _store = new InMemoryGameStore();
        _eventBus = new GameEventBus(NullLogger<GameEventBus>.Instance);
    }

    private static QuestDefinition CreateTestQuest(string id = "quest1", int completionBonus = 50, TimeSpan? timeLimit = null)
    {
        return new QuestDefinition
        {
            Id = id,
            Name = "Test Quest",
            Description = "A test quest",
            Steps = new[]
            {
                new QuestStep("step1", "Do step 1", 10),
                new QuestStep("step2", "Do step 2", 20),
                new QuestStep("step3", "Do step 3", 30)
            },
            CompletionBonus = completionBonus,
            TimeLimit = timeLimit
        };
    }

    private QuestEngine CreateEngine(params QuestDefinition[] quests)
    {
        return new QuestEngine(
            _store, _eventBus, quests.ToList(),
            NullLogger<QuestEngine>.Instance);
    }

    [Fact]
    public async Task AssignAsync_ShouldAssignQuestToPlayer()
    {
        var quest = CreateTestQuest();
        var engine = CreateEngine(quest);

        await engine.AssignAsync("user1", "quest1");

        var active = await engine.GetActiveAsync("user1");
        active.Should().HaveCount(1);
        active[0].Quest.Id.Should().Be("quest1");
    }

    [Fact]
    public async Task AssignAsync_UnknownQuest_ShouldThrow()
    {
        var engine = CreateEngine(CreateTestQuest());

        var act = () => engine.AssignAsync("user1", "unknown-quest");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*unknown-quest*");
    }

    [Fact]
    public async Task ProgressAsync_ShouldCompleteStep()
    {
        var quest = CreateTestQuest();
        var engine = CreateEngine(quest);
        await engine.AssignAsync("user1", "quest1");

        var progress = await engine.ProgressAsync("user1", "quest1", "step1");

        progress.CompletedCount.Should().Be(1);
        progress.TotalSteps.Should().Be(3);
        progress.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task ProgressAsync_AllSteps_ShouldCompleteQuest()
    {
        var quest = CreateTestQuest();
        var engine = CreateEngine(quest);
        await engine.AssignAsync("user1", "quest1");

        await engine.ProgressAsync("user1", "quest1", "step1");
        await engine.ProgressAsync("user1", "quest1", "step2");
        var finalProgress = await engine.ProgressAsync("user1", "quest1", "step3");

        finalProgress.IsCompleted.Should().BeTrue();
        finalProgress.CompletedCount.Should().Be(3);
    }

    [Fact]
    public async Task ProgressAsync_ShouldEmitQuestCompletedEvent()
    {
        var quest = CreateTestQuest(completionBonus: 50);
        var engine = CreateEngine(quest);
        await engine.AssignAsync("user1", "quest1");

        QuestCompletedEvent? capturedEvent = null;
        _eventBus.Subscribe<QuestCompletedEvent>(e =>
        {
            capturedEvent = e;
            return Task.CompletedTask;
        });

        await engine.ProgressAsync("user1", "quest1", "step1");
        await engine.ProgressAsync("user1", "quest1", "step2");
        await engine.ProgressAsync("user1", "quest1", "step3");

        capturedEvent.Should().NotBeNull();
        capturedEvent!.UserId.Should().Be("user1");
        capturedEvent.Quest.Id.Should().Be("quest1");
        // Total XP = step rewards (10 + 20 + 30) + completion bonus (50) = 110
        capturedEvent.TotalXpEarned.Should().Be(110);
    }

    [Fact]
    public async Task ProgressAsync_ShouldNotEmitEventBeforeAllStepsComplete()
    {
        var quest = CreateTestQuest();
        var engine = CreateEngine(quest);
        await engine.AssignAsync("user1", "quest1");

        QuestCompletedEvent? capturedEvent = null;
        _eventBus.Subscribe<QuestCompletedEvent>(e =>
        {
            capturedEvent = e;
            return Task.CompletedTask;
        });

        await engine.ProgressAsync("user1", "quest1", "step1");
        await engine.ProgressAsync("user1", "quest1", "step2");

        capturedEvent.Should().BeNull();
    }

    [Fact]
    public async Task ProgressAsync_UnknownStep_ShouldThrow()
    {
        var quest = CreateTestQuest();
        var engine = CreateEngine(quest);
        await engine.AssignAsync("user1", "quest1");

        var act = () => engine.ProgressAsync("user1", "quest1", "unknown-step");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*unknown-step*");
    }

    [Fact]
    public async Task GetProgressAsync_ShouldReturnNullForUnassignedQuest()
    {
        var quest = CreateTestQuest();
        var engine = CreateEngine(quest);

        var progress = await engine.GetProgressAsync("user1", "quest1");

        progress.Should().BeNull();
    }

    [Fact]
    public async Task GetProgressAsync_ShouldReturnProgressForAssignedQuest()
    {
        var quest = CreateTestQuest();
        var engine = CreateEngine(quest);
        await engine.AssignAsync("user1", "quest1");
        await engine.ProgressAsync("user1", "quest1", "step1");

        var progress = await engine.GetProgressAsync("user1", "quest1");

        progress.Should().NotBeNull();
        progress!.CompletedCount.Should().Be(1);
        progress.NextStep!.Id.Should().Be("step2");
    }

    [Fact]
    public async Task GetActiveAsync_ShouldReturnMultipleQuests()
    {
        var quest1 = CreateTestQuest("quest1");
        var quest2 = CreateTestQuest("quest2");
        quest2.Name = "Quest 2";
        var engine = CreateEngine(quest1, quest2);

        await engine.AssignAsync("user1", "quest1");
        await engine.AssignAsync("user1", "quest2");

        var active = await engine.GetActiveAsync("user1");

        active.Should().HaveCount(2);
    }

    [Fact]
    public async Task ProgressAsync_CompletingStepOutOfOrder_ShouldStillWork()
    {
        var quest = CreateTestQuest();
        var engine = CreateEngine(quest);
        await engine.AssignAsync("user1", "quest1");

        // Complete step3 before step1 and step2
        var progress = await engine.ProgressAsync("user1", "quest1", "step3");

        progress.CompletedCount.Should().Be(1);
        progress.IsCompleted.Should().BeFalse();
    }
}
