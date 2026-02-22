using FluentAssertions;
using GameifyKit.Achievements;
using Xunit;
using GameifyKit.Events;
using GameifyKit.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameifyKit.Tests.Achievements;

public class AchievementEngineTests
{
    private readonly InMemoryGameStore _store;
    private readonly GameEventBus _eventBus;

    public AchievementEngineTests()
    {
        _store = new InMemoryGameStore();
        _eventBus = new GameEventBus(NullLogger<GameEventBus>.Instance);
    }

    private AchievementEngine CreateEngine(params AchievementDefinition[] definitions)
    {
        return new AchievementEngine(
            _store, _eventBus, definitions.ToList(),
            NullLogger<AchievementEngine>.Instance);
    }

    [Fact]
    public async Task CheckAsync_CounterBased_ShouldUnlockWhenTargetReached()
    {
        var definition = new AchievementDefinition
        {
            Id = "quiz-master",
            Name = "Quiz Master",
            Counter = "quizzes_completed",
            Target = 3,
            XpReward = 100
        };
        var engine = CreateEngine(definition);

        for (int i = 0; i < 3; i++)
            await engine.IncrementAsync("user1", "quizzes_completed");

        var unlocked = await engine.CheckAsync("user1");

        unlocked.Should().HaveCount(1);
        unlocked[0].Id.Should().Be("quiz-master");
    }

    [Fact]
    public async Task CheckAsync_CounterBased_ShouldNotUnlockBeforeTarget()
    {
        var definition = new AchievementDefinition
        {
            Id = "quiz-master",
            Name = "Quiz Master",
            Counter = "quizzes_completed",
            Target = 5
        };
        var engine = CreateEngine(definition);

        for (int i = 0; i < 4; i++)
            await engine.IncrementAsync("user1", "quizzes_completed");

        var unlocked = await engine.CheckAsync("user1");

        unlocked.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckAsync_ConditionBased_ShouldUnlockWhenConditionMet()
    {
        var definition = new AchievementDefinition
        {
            Id = "high-scorer",
            Name = "High Scorer",
            Condition = ctx => ctx.GetStat("high_score") >= 1000
        };
        var engine = CreateEngine(definition);

        await engine.SetStatAsync("user1", "high_score", 1000);

        var unlocked = await engine.CheckAsync("user1");

        unlocked.Should().HaveCount(1);
        unlocked[0].Id.Should().Be("high-scorer");
    }

    [Fact]
    public async Task CheckAsync_ShouldNotUnlockSameAchievementTwice()
    {
        var definition = new AchievementDefinition
        {
            Id = "first-step",
            Name = "First Step",
            Counter = "actions",
            Target = 1
        };
        var engine = CreateEngine(definition);

        await engine.IncrementAsync("user1", "actions");

        var firstCheck = await engine.CheckAsync("user1");
        var secondCheck = await engine.CheckAsync("user1");

        firstCheck.Should().HaveCount(1);
        secondCheck.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckAsync_ShouldEmitAchievementUnlockedEvent()
    {
        var definition = new AchievementDefinition
        {
            Id = "first-step",
            Name = "First Step",
            Counter = "actions",
            Target = 1
        };
        var engine = CreateEngine(definition);

        AchievementUnlockedEvent? capturedEvent = null;
        _eventBus.Subscribe<AchievementUnlockedEvent>(e =>
        {
            capturedEvent = e;
            return Task.CompletedTask;
        });

        await engine.IncrementAsync("user1", "actions");
        await engine.CheckAsync("user1");

        capturedEvent.Should().NotBeNull();
        capturedEvent!.UserId.Should().Be("user1");
        capturedEvent.Achievement.Id.Should().Be("first-step");
    }

    [Fact]
    public async Task CheckAsync_SecretAchievement_ShouldStillBeUnlockable()
    {
        var definition = new AchievementDefinition
        {
            Id = "secret-one",
            Name = "Hidden Gem",
            Counter = "secrets_found",
            Target = 1,
            Secret = true
        };
        var engine = CreateEngine(definition);

        await engine.IncrementAsync("user1", "secrets_found");
        var unlocked = await engine.CheckAsync("user1");

        unlocked.Should().HaveCount(1);
        unlocked[0].Secret.Should().BeTrue();
    }

    [Fact]
    public async Task GetProgressAsync_ShouldReturnProgressForAllAchievements()
    {
        var definition = new AchievementDefinition
        {
            Id = "quiz-master",
            Name = "Quiz Master",
            Counter = "quizzes_completed",
            Target = 10
        };
        var engine = CreateEngine(definition);

        for (int i = 0; i < 5; i++)
            await engine.IncrementAsync("user1", "quizzes_completed");

        var progress = await engine.GetProgressAsync("user1");

        progress.Should().HaveCount(1);
        progress[0].Achievement.Id.Should().Be("quiz-master");
        progress[0].CurrentValue.Should().Be(5);
        progress[0].TargetValue.Should().Be(10);
        progress[0].Progress.Should().BeApproximately(0.5, 0.001);
        progress[0].Unlocked.Should().BeFalse();
    }

    [Fact]
    public async Task GetUnlockedAsync_ShouldReturnOnlyUnlockedAchievements()
    {
        var def1 = new AchievementDefinition { Id = "a1", Name = "A1", Counter = "c1", Target = 1 };
        var def2 = new AchievementDefinition { Id = "a2", Name = "A2", Counter = "c2", Target = 1 };
        var engine = CreateEngine(def1, def2);

        await engine.IncrementAsync("user1", "c1");
        await engine.CheckAsync("user1");

        var unlocked = await engine.GetUnlockedAsync("user1");

        unlocked.Should().HaveCount(1);
        unlocked[0].Id.Should().Be("a1");
    }

    [Fact]
    public async Task CheckAsync_MultipleAchievements_ShouldUnlockAllEligible()
    {
        var def1 = new AchievementDefinition { Id = "a1", Name = "A1", Counter = "c1", Target = 1 };
        var def2 = new AchievementDefinition { Id = "a2", Name = "A2", Counter = "c1", Target = 2 };
        var engine = CreateEngine(def1, def2);

        await engine.IncrementAsync("user1", "c1");
        await engine.IncrementAsync("user1", "c1");

        var unlocked = await engine.CheckAsync("user1");

        unlocked.Should().HaveCount(2);
        unlocked.Select(a => a.Id).Should().Contain("a1").And.Contain("a2");
    }

    [Fact]
    public async Task CheckAsync_ConditionThrowsException_ShouldNotCrash()
    {
        var definition = new AchievementDefinition
        {
            Id = "buggy",
            Name = "Buggy",
            Condition = _ => throw new InvalidOperationException("Boom")
        };
        var engine = CreateEngine(definition);

        var unlocked = await engine.CheckAsync("user1");

        unlocked.Should().BeEmpty();
    }
}
