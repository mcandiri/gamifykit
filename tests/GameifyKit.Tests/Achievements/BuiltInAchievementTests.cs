using FluentAssertions;
using GameifyKit.Achievements;
using Xunit;

namespace GameifyKit.Tests.Achievements;

public class BuiltInAchievementTests
{
    [Fact]
    public void FirstLogin_ShouldBeCorrectlyConfigured()
    {
        var achievement = BuiltInAchievements.FirstLogin;

        achievement.Id.Should().Be("first-login");
        achievement.Name.Should().Be("First Steps");
        achievement.Counter.Should().Be("logins");
        achievement.Target.Should().Be(1);
        achievement.XpReward.Should().Be(50);
        achievement.Tier.Should().Be(AchievementTier.Bronze);
        achievement.Secret.Should().BeFalse();
    }

    [Fact]
    public void Streak7_ShouldBeCorrectlyConfigured()
    {
        var achievement = BuiltInAchievements.Streak7;

        achievement.Id.Should().Be("streak-7");
        achievement.Name.Should().Be("Week Warrior");
        achievement.Counter.Should().Be("best_streak");
        achievement.Target.Should().Be(7);
        achievement.XpReward.Should().Be(150);
        achievement.Tier.Should().Be(AchievementTier.Silver);
    }

    [Fact]
    public void Streak30_ShouldBeCorrectlyConfigured()
    {
        var achievement = BuiltInAchievements.Streak30;

        achievement.Id.Should().Be("streak-30");
        achievement.Name.Should().Be("Monthly Legend");
        achievement.Counter.Should().Be("best_streak");
        achievement.Target.Should().Be(30);
        achievement.XpReward.Should().Be(500);
        achievement.Tier.Should().Be(AchievementTier.Gold);
    }

    [Fact]
    public void Level10_ShouldBeCorrectlyConfigured()
    {
        var achievement = BuiltInAchievements.Level10;

        achievement.Id.Should().Be("level-10");
        achievement.Name.Should().Be("Rising Star");
        achievement.Counter.Should().Be("level");
        achievement.Target.Should().Be(10);
        achievement.XpReward.Should().Be(200);
        achievement.Tier.Should().Be(AchievementTier.Silver);
    }

    [Fact]
    public void Level50_ShouldBeCorrectlyConfigured()
    {
        var achievement = BuiltInAchievements.Level50;

        achievement.Id.Should().Be("level-50");
        achievement.Name.Should().Be("Grandmaster");
        achievement.Counter.Should().Be("level");
        achievement.Target.Should().Be(50);
        achievement.XpReward.Should().Be(1000);
        achievement.Tier.Should().Be(AchievementTier.Platinum);
    }

    [Fact]
    public void AllBuiltIns_ShouldHaveUniqueIds()
    {
        var achievements = new[]
        {
            BuiltInAchievements.FirstLogin,
            BuiltInAchievements.Streak7,
            BuiltInAchievements.Streak30,
            BuiltInAchievements.Level10,
            BuiltInAchievements.Level50
        };

        var ids = achievements.Select(a => a.Id).ToList();
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AllBuiltIns_ShouldHaveNonEmptyNameAndDescription()
    {
        var achievements = new[]
        {
            BuiltInAchievements.FirstLogin,
            BuiltInAchievements.Streak7,
            BuiltInAchievements.Streak30,
            BuiltInAchievements.Level10,
            BuiltInAchievements.Level50
        };

        foreach (var ach in achievements)
        {
            ach.Name.Should().NotBeNullOrWhiteSpace($"because {ach.Id} should have a name");
            ach.Description.Should().NotBeNullOrWhiteSpace($"because {ach.Id} should have a description");
        }
    }

    [Fact]
    public void AllBuiltIns_ShouldHavePositiveXpReward()
    {
        var achievements = new[]
        {
            BuiltInAchievements.FirstLogin,
            BuiltInAchievements.Streak7,
            BuiltInAchievements.Streak30,
            BuiltInAchievements.Level10,
            BuiltInAchievements.Level50
        };

        foreach (var ach in achievements)
        {
            ach.XpReward.Should().BeGreaterThan(0, $"because {ach.Id} should reward XP");
        }
    }

    [Fact]
    public void AllBuiltIns_ShouldHaveCounterSet()
    {
        var achievements = new[]
        {
            BuiltInAchievements.FirstLogin,
            BuiltInAchievements.Streak7,
            BuiltInAchievements.Streak30,
            BuiltInAchievements.Level10,
            BuiltInAchievements.Level50
        };

        foreach (var ach in achievements)
        {
            ach.Counter.Should().NotBeNullOrWhiteSpace($"because {ach.Id} is counter-based");
            ach.Target.Should().BeGreaterThan(0, $"because {ach.Id} should have a positive target");
        }
    }
}
