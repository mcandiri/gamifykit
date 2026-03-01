# GameifyKit

> Gamification toolkit for .NET — XP, levels, achievements, quests, streaks, tiered leaderboards, virtual economy, and analytics.

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![CI](https://github.com/mcandiri/gamifykit/actions/workflows/ci.yml/badge.svg)](https://github.com/mcandiri/gamifykit/actions)
[![Tests](https://img.shields.io/badge/tests-161%20passed-brightgreen)](https://github.com/mcandiri/gamifykit/actions)

---

## Why GameifyKit?

Most apps that want engagement end up rebuilding XP, leaderboards, and achievements from scratch. GameifyKit packages those building blocks so you don't have to.

- **Single package** — XP, levels, achievements, quests, streaks, leaderboards, economy, analytics, anti-cheat
- **Quick setup** — `AddGameifyKit()` with sensible defaults
- **Storage-agnostic** — InMemory, SQL Server, or PostgreSQL out of the box
- **Event-driven** — React to level-ups, achievements, and tier changes in real-time
- **Thread-safe** — Built for concurrent workloads
- **High test coverage** — Core systems (XP calculation, achievement rules, streak logic, leaderboard ranking) are well-tested

---

## Quick Start

### Install

```bash
dotnet add package GameifyKit
```

### Setup

```csharp
builder.Services.AddGameifyKit(options =>
{
    options.UseInMemoryStore(); // or UseSqlServer() / UsePostgreSql()

    options.ConfigureLeveling(lvl =>
    {
        lvl.Curve = LevelCurve.Exponential;
        lvl.BaseXp = 100;
        lvl.Multiplier = 1.5;
        lvl.MaxLevel = 100;
    });
});
```

### Use

```csharp
public class MyService
{
    private readonly IGameEngine _game;
    public MyService(IGameEngine game) => _game = game;

    public async Task OnQuizCompleted(string userId, int score)
    {
        // Award XP (rules engine auto-checks limits & cooldowns)
        var result = await _game.Xp.AddAsync(userId, score * 10, "quiz-complete");
        // result.FinalXp = 1700, result.Level = 14, result.LeveledUp = true

        // Check achievements
        await _game.Achievements.IncrementAsync(userId, "quizzes_completed");
        var unlocked = await _game.Achievements.CheckAsync(userId);

        // Record streak
        var streak = await _game.Streaks.RecordAsync(userId, "daily-login");
        // streak.CurrentStreak = 8, streak.MilestoneReached = "Week Warrior"
    }
}
```

---

## Features

### XP & Leveling

Three curve types for XP progression:

| Curve | Description |
|-------|-------------|
| **Linear** | Same XP per level (e.g., 100 XP each) |
| **Exponential** | Each level needs `Multiplier`x more XP |
| **Custom** | Define exact thresholds per level |

```csharp
options.ConfigureLeveling(lvl =>
{
    lvl.Curve = LevelCurve.Exponential;
    lvl.BaseXp = 100;
    lvl.Multiplier = 1.5;
    lvl.MaxLevel = 100;
});
```

### Achievements

Built-in achievements + define your own with counters or custom conditions:

```csharp
options.ConfigureAchievements(ach =>
{
    ach.UseBuiltIn(BuiltInAchievements.FirstLogin);
    ach.UseBuiltIn(BuiltInAchievements.Streak7);

    ach.Define("quiz-master", a =>
    {
        a.Name = "Quiz Master";
        a.Counter = "quizzes_completed";
        a.Target = 50;
        a.XpReward = 500;
        a.Tier = AchievementTier.Gold;
    });

    ach.Define("night-owl", a =>
    {
        a.Name = "Night Owl";
        a.Condition = ctx => ctx.LastActivityTime.Hour is >= 0 and < 5;
        a.Secret = true; // Hidden until unlocked
    });
});
```

### Quests & Missions

Multi-step quests with time limits and recurring support:

```csharp
options.ConfigureQuests(q =>
{
    q.Define("onboarding", quest =>
    {
        quest.Name = "Getting Started";
        quest.Steps = new[]
        {
            new QuestStep("complete-profile", "Complete your profile", xpReward: 50),
            new QuestStep("first-quiz", "Take your first quiz", xpReward: 100),
        };
        quest.CompletionBonus = 500;
        quest.TimeLimit = TimeSpan.FromDays(7);
        quest.AutoAssign = true;
    });
});
```

### Streaks

Daily/weekly streaks with grace periods and milestone rewards:

```csharp
options.ConfigureStreaks(s =>
{
    s.Define("daily-login", streak =>
    {
        streak.Period = StreakPeriod.Daily;
        streak.GracePeriod = TimeSpan.FromHours(36);
        streak.Milestones = new[]
        {
            new StreakMilestone(7, xpBonus: 150, badge: "Week Warrior"),
            new StreakMilestone(30, xpBonus: 500, badge: "Monthly Legend"),
        };
    });
});
```

### Tiered Leaderboards

Bronze to Diamond tiers with daily/weekly/monthly/all-time periods:

```csharp
options.ConfigureLeaderboard(lb =>
{
    lb.Periods = new[] { LeaderboardPeriod.Weekly, LeaderboardPeriod.AllTime };
    lb.Tiers = new[]
    {
        new TierDefinition("bronze", "Bronze", maxPercentile: 0.50),
        new TierDefinition("silver", "Silver", maxPercentile: 0.75),
        new TierDefinition("gold", "Gold", maxPercentile: 0.90),
        new TierDefinition("diamond", "Diamond", maxPercentile: 1.00),
    };
    lb.PromotionBonusXp = 200;
});
```

### XP Boosts

Time-limited multipliers with stacking and max cap:

```csharp
await _game.Boosts.ActivateAsync(userId, new XpBoost
{
    Multiplier = 2.0,
    Duration = TimeSpan.FromHours(48),
    Reason = "weekend-bonus"
});

// XP awards automatically use active multipliers
var result = await _game.Xp.AddAsync(userId, 100, "quiz");
// result.Multiplier = 2.0, result.FinalXp = 200
```

### Virtual Economy

Currency earned from XP, reward shop with purchase limits:

```csharp
options.ConfigureEconomy(e =>
{
    e.CurrencyName = "coins";
    e.XpToCurrencyRatio = 10; // Every 10 XP = 1 coin

    e.DefineReward("extra-time", r =>
    {
        r.Name = "Extra Exam Time";
        r.Cost = 100;
        r.MaxPurchasesPerDay = 3;
    });
});
```

### Analytics

Track engagement metrics across your player base:

```csharp
var insights = await _game.Analytics.GetInsightsAsync();
// insights.DailyActiveUsers, insights.AverageXpPerPlayer, etc.
```

### Anti-Cheat

Daily XP caps, cooldowns, action limits, and suspicious activity alerts:

```csharp
options.ConfigureRules(rules =>
{
    rules.MaxDailyXp = 5000;
    rules.Cooldown("quiz-complete", TimeSpan.FromMinutes(2));
    rules.MaxActionsPerHour = 200;
});
```

### Event System

React to every state change in real-time:

```csharp
options.OnEvent<LevelUpEvent>(async e =>
{
    await SendNotification(e.UserId, $"Level {e.NewLevel}!");
});

options.OnEvent<AchievementUnlockedEvent>(async e =>
{
    await SendNotification(e.UserId, $"Unlocked: {e.Achievement.Name}!");
});

options.OnEvent<TierChangeEvent>(async e =>
{
    await SendNotification(e.UserId, $"Promoted to {e.NewTier.Name}!");
});
```

---

## What This Is (and Isn't)

GameifyKit is a **library**, not a platform. It gives you the building blocks:

- XP calculation, leveling curves, achievement rules, streak tracking, leaderboards
- You own the storage, the UI, and the integration

It does NOT:

- Provide a ready-made UI or dashboard
- Replace dedicated platforms like Badgeville or Bunchball
- Try to be a one-size-fits-all gamification SaaS

If you need a simple, testable gamification layer inside your existing .NET app — that's what this is for.

---

## Player Profile

Get a complete player state in a single call:

```csharp
var profile = await _game.GetProfileAsync(userId);
// profile.TotalXp, profile.Level, profile.CurrentLevelProgress
// profile.Tier, profile.Rank
// profile.Achievements, profile.ActiveQuests, profile.ActiveStreaks
// profile.ActiveBoosts, profile.Wallet, profile.Stats
```

---

## Storage

| Provider | Use Case | Setup |
|----------|----------|-------|
| **InMemory** | Testing, prototyping | `options.UseInMemoryStore()` |
| **SQL Server** | Production (MSSQL) | `options.UseSqlServer(connectionString)` |
| **PostgreSQL** | Production (Postgres) | `options.UsePostgreSql(connectionString)` |

Database stores auto-create tables (prefixed with `GameifyKit_`) on first use.

---

## Architecture

```
                    ┌──────────────────────────┐
                    │       IGameEngine        │
                    │    (Main Entry Point)     │
                    └────────────┬─────────────┘
                                 │
        ┌────────────────────────┼────────────────────────┐
        │            │           │           │             │
   ┌────┴────┐ ┌────┴────┐ ┌────┴────┐ ┌────┴────┐ ┌─────┴─────┐
   │XpEngine │ │Achieve- │ │ Quest   │ │ Streak  │ │Leaderboard│
   │         │ │ment     │ │ Engine  │ │ Engine  │ │  Engine   │
   └────┬────┘ │Engine   │ └────┬────┘ └────┬────┘ └─────┬─────┘
        │      └────┬────┘      │           │             │
        │           │           │           │             │
   ┌────┴────┐ ┌────┴────┐ ┌───┴────┐ ┌────┴────┐ ┌─────┴─────┐
   │  Boost  │ │Economy  │ │Analyt- │ │  Rule   │ │  Event    │
   │ Engine  │ │ Engine  │ │ics     │ │ Engine  │ │   Bus     │
   └────┬────┘ └────┬────┘ └───┬────┘ └────┬────┘ └─────┬─────┘
        │           │          │            │             │
        └───────────┴──────────┴────────────┴─────────────┘
                                 │
                    ┌────────────┴─────────────┐
                    │       IGameStore         │
                    │   (Storage Abstraction)   │
                    └──────────────────────────┘
                    │ InMemory │ SQL Server │ PostgreSQL │
```

---

## Background

> Built to increase student engagement on an education platform serving 1,500+ students. XP, streaks, and achievements were added incrementally based on what actually moved retention metrics — not as a monolithic "gamification framework" designed upfront.

---

## What GameifyKit Is NOT

| Need | Use Instead |
|------|-------------|
| Full game engine | Unity, Godot |
| Social features | Your own social layer |
| Push notifications | Your notification service |
| UI components | Your frontend framework |
| Real-money transactions | Payment processing service |

---

## Configuration Reference

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Leveling.Curve` | `LevelCurve` | `Exponential` | XP curve type |
| `Leveling.BaseXp` | `int` | `100` | Base XP for first level |
| `Leveling.Multiplier` | `double` | `1.5` | Exponential multiplier |
| `Leveling.MaxLevel` | `int` | `100` | Maximum level |
| `Boosts.MaxStackableBoosts` | `int` | `3` | Max simultaneous boosts |
| `Boosts.MaxMultiplier` | `double` | `5.0` | Max combined multiplier |
| `Economy.XpToCurrencyRatio` | `int` | `10` | XP per currency unit |
| `Rules.MaxDailyXp` | `int` | `5000` | Daily XP cap |
| `Rules.MaxActionsPerHour` | `int` | `200` | Hourly action limit |
| `Leaderboard.PromotionBonusXp` | `int` | `200` | XP bonus on tier promotion |

---

## API Endpoints (Sample)

The included sample API demonstrates real usage:

```
POST /api/game/{userId}/xp              — Add XP
GET  /api/game/{userId}/profile          — Full player profile
GET  /api/game/{userId}/achievements     — Player achievements
POST /api/game/{userId}/quests/progress  — Progress a quest
GET  /api/game/{userId}/quests           — Active quests
GET  /api/game/leaderboard/{period}      — Leaderboard
GET  /api/game/{userId}/standing         — Player standing
POST /api/game/{userId}/boost            — Activate boost
GET  /api/game/{userId}/wallet           — Wallet balance
POST /api/game/{userId}/purchase         — Purchase reward
GET  /api/game/analytics                 — Engagement insights
```

---

## Getting Started (Development)

```bash
# Clone the repository
git clone https://github.com/mcandiri/gamifykit.git
cd gamifykit

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run all tests (161 tests)
dotnet test

# Run the sample API
dotnet run --project samples/GameifyKit.SampleApi
# Open http://localhost:5000/swagger
```

---

## Roadmap

- [ ] Redis-backed leaderboard for high-scale scenarios
- [ ] Webhook notifications on events
- [ ] Admin dashboard (Blazor)
- [ ] Team/guild support
- [ ] A/B testing for gamification rules
- [ ] Seasonal events system

---

## Contributing

Contributions are welcome! Please open an issue first to discuss what you would like to change.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## License

[MIT](LICENSE)
