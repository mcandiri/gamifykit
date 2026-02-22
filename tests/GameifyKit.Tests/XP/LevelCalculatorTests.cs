using FluentAssertions;
using GameifyKit.Configuration;
using Xunit;
using GameifyKit.XP;

namespace GameifyKit.Tests.XP;

public class LevelCalculatorTests
{
    [Fact]
    public void GetLevel_WithLinearCurve_ShouldReturnCorrectLevel()
    {
        var config = new LevelingConfig
        {
            Curve = LevelCurve.Linear,
            BaseXp = 100,
            MaxLevel = 50
        };
        var calc = new LevelCalculator(config);

        calc.GetLevel(0).Should().Be(1);
        calc.GetLevel(50).Should().Be(1);
        calc.GetLevel(100).Should().Be(2);
        calc.GetLevel(199).Should().Be(2);
        calc.GetLevel(200).Should().Be(3);
        calc.GetLevel(500).Should().Be(6);
    }

    [Fact]
    public void GetLevel_WithExponentialCurve_ShouldReturnCorrectLevel()
    {
        var config = new LevelingConfig
        {
            Curve = LevelCurve.Exponential,
            BaseXp = 100,
            Multiplier = 1.5,
            MaxLevel = 50
        };
        var calc = new LevelCalculator(config);

        calc.GetLevel(0).Should().Be(1);
        // Level 2 requires 100 XP (BaseXp * 1.5^0 = 100)
        calc.GetLevel(100).Should().Be(2);
        // Level 3 requires 150 XP (BaseXp * 1.5^1 = 150), total = 250
        calc.GetLevel(250).Should().Be(3);
    }

    [Fact]
    public void GetLevel_WithCustomCurve_ShouldReturnCorrectLevel()
    {
        var config = new LevelingConfig
        {
            Curve = LevelCurve.Custom,
            BaseXp = 100,
            MaxLevel = 5,
            CustomThresholds = new[] { 100, 300, 600, 1000 }
        };
        var calc = new LevelCalculator(config);

        calc.GetLevel(0).Should().Be(1);
        calc.GetLevel(99).Should().Be(1);
        calc.GetLevel(100).Should().Be(2);
        calc.GetLevel(300).Should().Be(3);
        calc.GetLevel(600).Should().Be(4);
        calc.GetLevel(1000).Should().Be(5);
    }

    [Fact]
    public void GetLevel_ShouldNotExceedMaxLevel()
    {
        var config = new LevelingConfig
        {
            Curve = LevelCurve.Linear,
            BaseXp = 100,
            MaxLevel = 5
        };
        var calc = new LevelCalculator(config);

        calc.GetLevel(999999).Should().Be(5);
    }

    [Fact]
    public void GetLevel_WithNegativeXp_ShouldReturnLevel1()
    {
        var config = new LevelingConfig
        {
            Curve = LevelCurve.Linear,
            BaseXp = 100,
            MaxLevel = 50
        };
        var calc = new LevelCalculator(config);

        calc.GetLevel(-100).Should().Be(1);
    }

    [Fact]
    public void GetXpForLevel_Linear_ShouldReturnBaseXp()
    {
        var config = new LevelingConfig
        {
            Curve = LevelCurve.Linear,
            BaseXp = 100,
            MaxLevel = 50
        };
        var calc = new LevelCalculator(config);

        calc.GetXpForLevel(1).Should().Be(0);
        calc.GetXpForLevel(2).Should().Be(100);
        calc.GetXpForLevel(5).Should().Be(100);
        calc.GetXpForLevel(10).Should().Be(100);
    }

    [Fact]
    public void GetXpForLevel_Exponential_ShouldIncreaseWithLevel()
    {
        var config = new LevelingConfig
        {
            Curve = LevelCurve.Exponential,
            BaseXp = 100,
            Multiplier = 2.0,
            MaxLevel = 50
        };
        var calc = new LevelCalculator(config);

        calc.GetXpForLevel(2).Should().Be(100); // 100 * 2^0
        calc.GetXpForLevel(3).Should().Be(200); // 100 * 2^1
        calc.GetXpForLevel(4).Should().Be(400); // 100 * 2^2
    }

    [Fact]
    public void GetTotalXpForLevel_Linear_ShouldBeConsistent()
    {
        var config = new LevelingConfig
        {
            Curve = LevelCurve.Linear,
            BaseXp = 100,
            MaxLevel = 50
        };
        var calc = new LevelCalculator(config);

        calc.GetTotalXpForLevel(1).Should().Be(0);
        calc.GetTotalXpForLevel(2).Should().Be(100);
        calc.GetTotalXpForLevel(3).Should().Be(200);
        calc.GetTotalXpForLevel(6).Should().Be(500);
    }

    [Fact]
    public void GetProgress_ShouldReturnProgressTowardNextLevel()
    {
        var config = new LevelingConfig
        {
            Curve = LevelCurve.Linear,
            BaseXp = 100,
            MaxLevel = 50
        };
        var calc = new LevelCalculator(config);

        calc.GetProgress(0).Should().Be(0.0);
        calc.GetProgress(50).Should().BeApproximately(0.5, 0.001);
        calc.GetProgress(100).Should().Be(0.0); // Just hit level 2, 0 progress toward level 3
    }

    [Fact]
    public void GetProgress_AtMaxLevel_ShouldReturn1()
    {
        var config = new LevelingConfig
        {
            Curve = LevelCurve.Linear,
            BaseXp = 100,
            MaxLevel = 3
        };
        var calc = new LevelCalculator(config);

        // Level 3 requires total XP = 200
        calc.GetProgress(200).Should().Be(1.0);
        calc.GetProgress(99999).Should().Be(1.0);
    }

    [Fact]
    public void GetXpForLevel_Custom_ShouldFallBackToLastThreshold()
    {
        var config = new LevelingConfig
        {
            Curve = LevelCurve.Custom,
            BaseXp = 50,
            MaxLevel = 10,
            CustomThresholds = new[] { 100, 300 }
        };
        var calc = new LevelCalculator(config);

        // Beyond the defined thresholds, it should use the difference of the last entry
        // Level 4 would be beyond custom thresholds, so uses last threshold's delta
        var xpForLevel4 = calc.GetXpForLevel(4);
        xpForLevel4.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Constructor_WithNullConfig_ShouldThrow()
    {
        var act = () => new LevelCalculator(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
