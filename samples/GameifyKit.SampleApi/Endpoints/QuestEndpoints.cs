using GameifyKit.SampleApi.Models;

namespace GameifyKit.SampleApi.Endpoints;

/// <summary>
/// Quest-related API endpoints.
/// </summary>
public static class QuestEndpoints
{
    /// <summary>
    /// Maps quest endpoints.
    /// </summary>
    public static void MapQuestEndpoints(this WebApplication app)
    {
        app.MapGet("/api/game/{userId}/quests", async (string userId, IGameEngine game, CancellationToken ct) =>
        {
            var quests = await game.Quests.GetActiveAsync(userId, ct);
            return Results.Ok(quests);
        })
        .WithName("GetActiveQuests")
        .WithTags("Quests");

        app.MapPost("/api/game/{userId}/quests/progress", async (string userId, QuestProgressRequest request, IGameEngine game, CancellationToken ct) =>
        {
            var progress = await game.Quests.ProgressAsync(userId, request.QuestId, request.StepId, ct);
            return Results.Ok(progress);
        })
        .WithName("ProgressQuest")
        .WithTags("Quests");
    }
}
