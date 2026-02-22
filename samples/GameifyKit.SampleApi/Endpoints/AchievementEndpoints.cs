namespace GameifyKit.SampleApi.Endpoints;

/// <summary>
/// Achievement-related API endpoints.
/// </summary>
public static class AchievementEndpoints
{
    /// <summary>
    /// Maps achievement endpoints.
    /// </summary>
    public static void MapAchievementEndpoints(this WebApplication app)
    {
        app.MapGet("/api/game/{userId}/achievements", async (string userId, IGameEngine game, CancellationToken ct) =>
        {
            var progress = await game.Achievements.GetProgressAsync(userId, ct);
            return Results.Ok(progress);
        })
        .WithName("GetAchievements")
        .WithTags("Achievements");

        app.MapPost("/api/game/{userId}/achievements/check", async (string userId, IGameEngine game, CancellationToken ct) =>
        {
            var unlocked = await game.Achievements.CheckAsync(userId, ct);
            return Results.Ok(unlocked);
        })
        .WithName("CheckAchievements")
        .WithTags("Achievements");
    }
}
