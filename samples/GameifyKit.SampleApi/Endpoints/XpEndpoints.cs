using GameifyKit.SampleApi.Models;

namespace GameifyKit.SampleApi.Endpoints;

/// <summary>
/// XP-related API endpoints.
/// </summary>
public static class XpEndpoints
{
    /// <summary>
    /// Maps XP endpoints.
    /// </summary>
    public static void MapXpEndpoints(this WebApplication app)
    {
        app.MapPost("/api/game/{userId}/xp", async (string userId, AddXpRequest request, IGameEngine game, CancellationToken ct) =>
        {
            var result = await game.Xp.AddAsync(userId, request.Amount, request.Action, ct);
            return Results.Ok(result);
        })
        .WithName("AddXp")
        .WithTags("XP");
    }
}
