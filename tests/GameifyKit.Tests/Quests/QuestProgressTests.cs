using FluentAssertions;
using GameifyKit.Quests;
using Xunit;

namespace GameifyKit.Tests.Quests;

public class QuestProgressTests
{
    private static QuestDefinition CreateTestQuest(TimeSpan? timeLimit = null)
    {
        return new QuestDefinition
        {
            Id = "quest1",
            Name = "Test Quest",
            Steps = new[]
            {
                new QuestStep("step1", "Step 1", 10),
                new QuestStep("step2", "Step 2", 20),
                new QuestStep("step3", "Step 3", 30)
            },
            TimeLimit = timeLimit
        };
    }

    [Fact]
    public void IsCompleted_NoStepsCompleted_ShouldBeFalse()
    {
        var progress = new QuestProgress
        {
            Quest = CreateTestQuest(),
            CompletedSteps = new HashSet<string>(),
            StartedAt = DateTimeOffset.UtcNow
        };

        progress.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void IsCompleted_AllStepsCompleted_ShouldBeTrue()
    {
        var progress = new QuestProgress
        {
            Quest = CreateTestQuest(),
            CompletedSteps = new HashSet<string> { "step1", "step2", "step3" },
            StartedAt = DateTimeOffset.UtcNow
        };

        progress.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void IsCompleted_PartiallyCompleted_ShouldBeFalse()
    {
        var progress = new QuestProgress
        {
            Quest = CreateTestQuest(),
            CompletedSteps = new HashSet<string> { "step1", "step2" },
            StartedAt = DateTimeOffset.UtcNow
        };

        progress.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void TotalSteps_ShouldMatchQuestStepCount()
    {
        var progress = new QuestProgress
        {
            Quest = CreateTestQuest(),
            CompletedSteps = new HashSet<string>(),
            StartedAt = DateTimeOffset.UtcNow
        };

        progress.TotalSteps.Should().Be(3);
    }

    [Fact]
    public void CompletedCount_ShouldReflectCompletedSteps()
    {
        var progress = new QuestProgress
        {
            Quest = CreateTestQuest(),
            CompletedSteps = new HashSet<string> { "step1", "step3" },
            StartedAt = DateTimeOffset.UtcNow
        };

        progress.CompletedCount.Should().Be(2);
    }

    [Fact]
    public void NextStep_ShouldReturnFirstIncompleteStep()
    {
        var progress = new QuestProgress
        {
            Quest = CreateTestQuest(),
            CompletedSteps = new HashSet<string> { "step1" },
            StartedAt = DateTimeOffset.UtcNow
        };

        progress.NextStep.Should().NotBeNull();
        progress.NextStep!.Id.Should().Be("step2");
    }

    [Fact]
    public void NextStep_AllCompleted_ShouldBeNull()
    {
        var progress = new QuestProgress
        {
            Quest = CreateTestQuest(),
            CompletedSteps = new HashSet<string> { "step1", "step2", "step3" },
            StartedAt = DateTimeOffset.UtcNow
        };

        progress.NextStep.Should().BeNull();
    }

    [Fact]
    public void TimeRemaining_WithNoTimeLimit_ShouldBeNull()
    {
        var progress = new QuestProgress
        {
            Quest = CreateTestQuest(timeLimit: null),
            CompletedSteps = new HashSet<string>(),
            StartedAt = DateTimeOffset.UtcNow
        };

        progress.TimeRemaining.Should().BeNull();
    }

    [Fact]
    public void TimeRemaining_WithTimeLimit_ShouldReturnPositiveValue()
    {
        var progress = new QuestProgress
        {
            Quest = CreateTestQuest(timeLimit: TimeSpan.FromHours(2)),
            CompletedSteps = new HashSet<string>(),
            StartedAt = DateTimeOffset.UtcNow
        };

        progress.TimeRemaining.Should().NotBeNull();
        progress.TimeRemaining!.Value.Should().BeGreaterThan(TimeSpan.Zero);
        progress.TimeRemaining.Value.Should().BeLessThanOrEqualTo(TimeSpan.FromHours(2));
    }

    [Fact]
    public void TimeRemaining_WhenExpired_ShouldReturnZero()
    {
        var progress = new QuestProgress
        {
            Quest = CreateTestQuest(timeLimit: TimeSpan.FromMinutes(30)),
            CompletedSteps = new HashSet<string>(),
            StartedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };

        progress.TimeRemaining.Should().NotBeNull();
        progress.TimeRemaining!.Value.Should().Be(TimeSpan.Zero);
    }
}
