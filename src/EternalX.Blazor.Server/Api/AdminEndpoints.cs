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

    private static bool IsOwner(HttpContext http, IConfiguration config)
    {
        var adminEmail = config["Authorization:AdminEmail"] ?? config["Authorization__AdminEmail"];
        return AdminAccess.IsAdmin(http.User, adminEmail);
    }
}
