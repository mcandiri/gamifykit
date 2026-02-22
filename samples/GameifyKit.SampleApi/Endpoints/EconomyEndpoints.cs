using GameifyKit.Boosts;
using GameifyKit.SampleApi.Models;

namespace GameifyKit.SampleApi.Endpoints;

/// <summary>
/// Economy and boost-related API endpoints.
/// </summary>
public static class EconomyEndpoints
{
    /// <summary>
    /// Maps economy and boost endpoints.
    /// </summary>
    public static void MapEconomyEndpoints(this WebApplication app)
    {
        app.MapGet("/api/game/{userId}/wallet", async (string userId, IGameEngine game, CancellationToken ct) =>
        {
            var wallet = await game.Economy.GetWalletAsync(userId, ct);
            return Results.Ok(wallet);
        })
        .WithName("GetWallet")
        .WithTags("Economy");

        app.MapPost("/api/game/{userId}/purchase", async (string userId, PurchaseRequest request, IGameEngine game, CancellationToken ct) =>
        {
            var result = await game.Economy.PurchaseAsync(userId, request.RewardId, ct);
            return Results.Ok(result);
        })
        .WithName("PurchaseReward")
        .WithTags("Economy");

        app.MapPost("/api/game/{userId}/boost", async (string userId, ActivateBoostRequest request, IGameEngine game, CancellationToken ct) =>
        {
            await game.Boosts.ActivateAsync(userId, new XpBoost
            {
                Multiplier = request.Multiplier,
                Duration = TimeSpan.FromHours(request.DurationHours),
                Reason = request.Reason
            }, ct);
            return Results.Ok(new { Message = "Boost activated" });
        })
        .WithName("ActivateBoost")
        .WithTags("Boosts");

        app.MapGet("/api/game/{userId}/profile", async (string userId, IGameEngine game, CancellationToken ct) =>
        {
            var profile = await game.GetProfileAsync(userId, ct);
            return Results.Ok(profile);
        })
        .WithName("GetProfile")
        .WithTags("Profile");

        app.MapGet("/api/game/analytics", async (IGameEngine game, CancellationToken ct) =>
        {
            var insights = await game.Analytics.GetInsightsAsync(ct);
            return Results.Ok(insights);
        })
        .WithName("GetAnalytics")
        .WithTags("Analytics");
    }
}
