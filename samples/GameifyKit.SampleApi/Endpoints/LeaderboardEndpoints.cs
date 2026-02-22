using GameifyKit.Leaderboard;

namespace GameifyKit.SampleApi.Endpoints;

/// <summary>
/// Leaderboard-related API endpoints.
/// </summary>
public static class LeaderboardEndpoints
{
    /// <summary>
    /// Maps leaderboard endpoints.
    /// </summary>
    public static void MapLeaderboardEndpoints(this WebApplication app)
    {
        app.MapGet("/api/game/leaderboard/{period}", async (LeaderboardPeriod period, int? count, IGameEngine game, CancellationToken ct) =>
        {
            var entries = await game.Leaderboard.GetTopAsync(period, count ?? 10, ct);
            return Results.Ok(entries);
        })
        .WithName("GetLeaderboard")
        .WithTags("Leaderboard");

        app.MapGet("/api/game/{userId}/standing", async (string userId, IGameEngine game, CancellationToken ct) =>
        {
            var standing = await game.Leaderboard.GetStandingAsync(userId, LeaderboardPeriod.Weekly, ct);
            return Results.Ok(standing);
        })
        .WithName("GetStanding")
        .WithTags("Leaderboard");
    }
}
