using System.Text.Json;
using EternalX.Blazor.Server.Auth;
using EternalX.Blazor.Server.Data;
using EternalX.Blazor.Server.Services;
using EternalX.Blazor.Shared;
using EternalX.Blazor.Shared.Models;

namespace EternalX.Blazor.Server.Api;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin").RequireAuthorization();

        group.MapGet("/stats", (HttpContext http, LiteDbService db, IConfiguration config) =>
        {
            if (!IsOwner(http, config)) return Results.Forbid();
            var settings = db.GetSettings();
            return Results.Ok(new
            {
                postCount = db.CountPosts(),
                replyCount = db.CountReplies(),
                figureCount = db.GetFigures().Count,
                enabledFigures = db.GetFigures(enabledOnly: true).Count,
                autoReplyPaused = settings.AutoReplyPaused
            });
        });

        /// <summary>Engagement by historical personality (likes, reshares, replies, @mentions).</summary>
        group.MapGet("/personalities/engagement", (HttpContext http, LiteDbService db, IConfiguration config) =>
        {
            if (!IsOwner(http, config)) return Results.Forbid();

            var figures = db.GetFigures();
            var posts = db.GetRecentPosts(count: 5000).ToList();
            var rows = PersonalityEngagementCalculator.Calculate(figures, posts);

            return Results.Ok(new
            {
                generatedAt = DateTime.UtcNow,
                postSampleSize = posts.Count,
                personalities = rows
            });
        });


        // AI agents (providers): enable/disable without removing Octopus API keys.
        group.MapGet("/agents", (HttpContext http, AiService ai, LiteDbService db, IConfiguration config) =>
        {
            if (!IsOwner(http, config)) return Results.Forbid();
            var settings = db.GetSettings();
            return Results.Ok(new
            {
                defaultProvider = settings.DefaultAiProvider
                                  ?? config["DEFAULT_AI_PROVIDER"]
                                  ?? "grok",
                agents = ai.GetAgentStatuses()
            });
        });

        group.MapPost("/agents/{name}/enable", async (
            HttpContext http,
            string name,
            LiteDbService db,
            IConfiguration config,
            IFeedNotifier notifier) =>
        {
            if (!IsOwner(http, config)) return Results.Forbid();
            if (!IsKnownAgent(name))
                return Results.BadRequest($"Unknown agent '{name}'. Known: {string.Join(", ", AiService.KnownProviderNames)}");

            var settings = db.GetSettings();
            settings.SetProviderEnabled(name, enabled: true);
            db.SaveSettings(settings);
            await notifier.NotifyAsync(FeedEvents.KindSettings).ConfigureAwait(false);
            return Results.Ok(new { name = name.ToLowerInvariant(), enabled = true });
        });

        group.MapPost("/agents/{name}/disable", async (
            HttpContext http,
            string name,
            LiteDbService db,
            IConfiguration config,
            IFeedNotifier notifier) =>
        {
            if (!IsOwner(http, config)) return Results.Forbid();
            if (!IsKnownAgent(name))
                return Results.BadRequest($"Unknown agent '{name}'. Known: {string.Join(", ", AiService.KnownProviderNames)}");

            var settings = db.GetSettings();
            settings.SetProviderEnabled(name, enabled: false);
            db.SaveSettings(settings);
            await notifier.NotifyAsync(FeedEvents.KindSettings).ConfigureAwait(false);
            return Results.Ok(new { name = name.ToLowerInvariant(), enabled = false });
        });

        group.MapPost("/agents/default", async (
            HttpContext http,
            SetDefaultAgentBody body,
            LiteDbService db,
            IConfiguration config,
            IFeedNotifier notifier) =>
        {
            if (!IsOwner(http, config)) return Results.Forbid();
            if (body?.Name is null || !IsKnownAgent(body.Name))
                return Results.BadRequest("Provide a known agent name.");

            var settings = db.GetSettings();
            settings.DefaultAiProvider = body.Name.Trim().ToLowerInvariant();
            // Default implies enabled so generation can use it.
            settings.SetProviderEnabled(settings.DefaultAiProvider, enabled: true);
            db.SaveSettings(settings);
            await notifier.NotifyAsync(FeedEvents.KindSettings).ConfigureAwait(false);
            return Results.Ok(new { defaultProvider = settings.DefaultAiProvider });
        });


        group.MapPost("/auto-reply/pause", async (HttpContext http, LiteDbService db, IConfiguration config, IFeedNotifier notifier) =>
        {
            if (!IsOwner(http, config)) return Results.Forbid();
            var settings = db.GetSettings();
            settings.AutoReplyPaused = true;
            db.SaveSettings(settings);
            await notifier.NotifyAsync(FeedEvents.KindSettings).ConfigureAwait(false);
            return Results.Ok(new { paused = true });
        });

        group.MapPost("/auto-reply/resume", async (HttpContext http, LiteDbService db, IConfiguration config, IFeedNotifier notifier) =>
        {
            if (!IsOwner(http, config)) return Results.Forbid();
            var settings = db.GetSettings();
            settings.AutoReplyPaused = false;
            db.SaveSettings(settings);
            await notifier.NotifyAsync(FeedEvents.KindSettings).ConfigureAwait(false);
            return Results.Ok(new { paused = false });
        });

        group.MapPost("/seed", (HttpContext http, LiteDbService db, IConfiguration config) =>
        {
            if (!IsOwner(http, config)) return Results.Forbid();
            db.EnsureSeeded();
            return Results.Ok(new
            {
                figures = db.GetFigures().Count,
                peerGroups = db.GetPeerGroups().Count
            });
        });

        group.MapGet("/export", (HttpContext http, LiteDbService db, IConfiguration config) =>
        {
            if (!IsOwner(http, config)) return Results.Forbid();
            var bundle = db.CreateExport();
            var json = JsonSerializer.Serialize(bundle, new JsonSerializerOptions { WriteIndented = true });
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            var name = $"eternalx-export-{DateTime.UtcNow:yyyyMMddHHmmss}.json";
            return Results.File(bytes, "application/json", name);
        });

        group.MapPost("/restore", async (HttpContext http, LiteDbService db, IConfiguration config) =>
        {
            if (!IsOwner(http, config)) return Results.Forbid();
            ExportBundle? bundle;
            try
            {
                bundle = await JsonSerializer.DeserializeAsync<ExportBundle>(http.Request.Body);
            }
            catch
            {
                return Results.BadRequest("Malformed export bundle.");
            }

            if (bundle is null)
                return Results.BadRequest("Empty bundle.");

            try
            {
                db.RestoreExport(bundle);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }

            return Results.Ok(new { restored = true, posts = db.CountPosts() });
        });

        group.MapPost("/clear-feed", async (HttpContext http, LiteDbService db, IConfiguration config, IFeedNotifier notifier) =>
        {
            if (!IsOwner(http, config)) return Results.Forbid();
            db.ClearFeed();
            await notifier.NotifyAsync(FeedEvents.KindFeedCleared).ConfigureAwait(false);
            return Results.Ok(new
            {
                cleared = true,
                figuresRemaining = db.GetFigures().Count
            });
        });

        return app;
    }

    public sealed record SetDefaultAgentBody(string Name);

    private static bool IsKnownAgent(string? name) =>
        !string.IsNullOrWhiteSpace(name) &&
        AiService.KnownProviderNames.Any(k =>
            string.Equals(k, name.Trim(), StringComparison.OrdinalIgnoreCase));

    private static bool IsOwner(HttpContext http, IConfiguration config)
    {
        var adminEmail = config["Authorization:AdminEmail"] ?? config["Authorization__AdminEmail"];
        return AdminAccess.IsAdmin(http.User, adminEmail);
    }
}
