using GameifyKit.Configuration;

namespace GameifyKit.XP;

/// <summary>
/// Calculates level from total XP based on the configured curve.
/// </summary>
public sealed class LevelCalculator
{
    private readonly LevelingConfig _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="LevelCalculator"/> class.
    /// </summary>
    public LevelCalculator(LevelingConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Gets the level for a given total XP value.
    /// </summary>
    public int GetLevel(long totalXp)
    {
        if (totalXp <= 0) return 1;

        // For custom curves, thresholds are cumulative total XP values
        if (_config.Curve == LevelCurve.Custom && _config.CustomThresholds is { Length: > 0 })
        {
            int level = 1;
            for (int i = 0; i < _config.CustomThresholds.Length && level < _config.MaxLevel; i++)
            {
                if (totalXp >= _config.CustomThresholds[i])
                    level = i + 2; // threshold[0] => level 2, threshold[1] => level 3, etc.
                else
                    break;
            }
            return Math.Min(level, _config.MaxLevel);
        }

        int lvl = 1;
        long accumulated = 0;

        while (lvl < _config.MaxLevel)
        {
            long required = GetXpForLevel(lvl + 1);
            if (accumulated + required > totalXp)
                break;
            accumulated += required;
            lvl++;
        }

        return lvl;
    }

    /// <summary>
    /// Gets the XP required to reach a specific level from the previous level.
    /// </summary>
    public long GetXpForLevel(int level)
    {
        if (level <= 1) return 0;

        return _config.Curve switch
        {
            LevelCurve.Linear => _config.BaseXp,
            LevelCurve.Exponential => (long)(_config.BaseXp * Math.Pow(_config.Multiplier, level - 2)),
            LevelCurve.Custom => GetCustomThreshold(level),
            _ => _config.BaseXp
        };
    }

    /// <summary>
    /// Gets the total XP required to reach a specific level from level 1.
    /// </summary>
    public long GetTotalXpForLevel(int level)
    {
        if (level <= 1) return 0;

        // For custom curves, thresholds are cumulative
        if (_config.Curve == LevelCurve.Custom && _config.CustomThresholds is { Length: > 0 })
        {
            int index = level - 2; // level 2 => index 0, level 3 => index 1
            if (index < 0) return 0;
            if (index >= _config.CustomThresholds.Length)
                return _config.CustomThresholds[^1];
            return _config.CustomThresholds[index];
        }

        long total = 0;
        for (int i = 2; i <= level; i++)
        {
            total += GetXpForLevel(i);
        }
        return total;
    }

    /// <summary>
    /// Gets the progress percentage toward the next level (0.0 to 1.0).
    /// </summary>
    public double GetProgress(long totalXp)
    {
        int currentLevel = GetLevel(totalXp);
        if (currentLevel >= _config.MaxLevel) return 1.0;

        long xpForCurrent = GetTotalXpForLevel(currentLevel);
        long xpForNext = GetTotalXpForLevel(currentLevel + 1);
        long range = xpForNext - xpForCurrent;

        if (range <= 0) return 1.0;

        return (double)(totalXp - xpForCurrent) / range;
    }

    private long GetCustomThreshold(int level)
    {
        if (_config.CustomThresholds == null || _config.CustomThresholds.Length == 0)
            return _config.BaseXp;

        int index = level - 1;
        if (index >= _config.CustomThresholds.Length)
            return _config.CustomThresholds[^1];

        return index > 0
            ? _config.CustomThresholds[index] - _config.CustomThresholds[index - 1]
            : _config.CustomThresholds[index];
    }
}
