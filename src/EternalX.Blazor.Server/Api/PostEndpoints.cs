using System.Security.Claims;
using EternalX.Blazor.Server.Auth;
using EternalX.Blazor.Server.Data;
using EternalX.Blazor.Server.Services;
using EternalX.Blazor.Shared;
using EternalX.Blazor.Shared.Models;

namespace EternalX.Blazor.Server.Api;

public static class PostEndpoints
{
    public sealed record CreatePostBody(string Content, string? Title = null);
    public sealed record CreateReplyBody(string Content);
    public sealed record VoteBody(int Value);

    public static IEndpointRouteBuilder MapPostEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/posts", (LiteDbService db, int? count) =>
            Results.Ok(db.GetRecentPosts(count is > 0 ? count.Value : 50)));

        app.MapGet("/api/posts/{id:guid}", (LiteDbService db, Guid id) =>
        {
            var post = db.GetPost(id);
            return post is null ? Results.NotFound() : Results.Ok(post);
        });

        app.MapGet("/api/me", (HttpContext http, LiteDbService db, IConfiguration config) =>
        {
            var authed = http.User.Identity?.IsAuthenticated ?? false;
            string? userId = null;
            string? name = null;
            string? email = null;
            var banned = false;
            if (authed)
            {
                userId = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                name = http.User.FindFirst(ClaimTypes.Name)?.Value ?? http.User.Identity?.Name;
                email = http.User.FindFirst(ClaimTypes.Email)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    var user = db.GetUser(userId);
                    banned = user?.IsBanned == true;
                    db.UpsertUser(new User
                    {
                        Id = userId,
                        DisplayName = name ?? userId,
                        Email = email,
                        Provider = "gateway",
                        IsBanned = banned,
                        BannedAt = user?.BannedAt,
                        BanReason = user?.BanReason,
                        LastIp = user?.LastIp
                    });
                }
            }

            var adminEmail = config["Authorization:AdminEmail"] ?? config["Authorization__AdminEmail"];
            return Results.Ok(new
            {
                authenticated = authed,
                userId,
                name,
                email,
                isAdmin = AdminAccess.IsAdmin(http.User, adminEmail),
                isBanned = banned,
                gateway = true
            });
        });

        app.MapGet("/api/ai/status", (LiteDbService db, AiService ai, IConfiguration config) =>
        {
            var settings = db.GetSettings();
            var live = ai.LiveProviderNames();
            return Results.Ok(new
            {
                paused = settings.AutoReplyPaused,
                providers = ai.ConfiguredProviderNames(),
                liveProviders = live,
                keyedProviders = ai.KeyedProviderNames(),
                disabledProviders = settings.DisabledProviders,
                live = live.Count > 0,
                usingStub = live.Count == 0,
                defaultProvider = settings.DefaultAiProvider
                                  ?? config["DEFAULT_AI_PROVIDER"]
                                  ?? "grok",
                figureCount = db.GetFigures(enabledOnly: true).Count,
                postCount = db.CountPosts(),
                signalR = FeedEvents.HubPath
            });
        });

        app.MapPost("/api/posts", async (
            LiteDbService db,
            ModeratorService moderator,
            DeepThreadService deepThread,
            IFeedNotifier notifier,
            HttpContext http,
            CreatePostBody body,
            CancellationToken ct) =>
        {
            var userId = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            if (db.IsUserBanned(userId))
                return Results.Json(new { error = "You are banned from posting." }, statusCode: StatusCodes.Status403Forbidden);

            if (string.IsNullOrWhiteSpace(body.Content))
                return Results.BadRequest("Say something.");
            if (body.Content.Length > 280)
                return Results.BadRequest("280 characters max.");
            if (body.Title is { Length: > 100 })
                return Results.BadRequest("Title is too long.");

            var ip = http.Connection.RemoteIpAddress?.ToString();
            var check = moderator.EvaluateAndRecord(body.Content, userId, ip);
            if (!check.Allowed)
                return Results.UnprocessableEntity(check.Reason);

            var author = http.User.FindFirst(ClaimTypes.Name)?.Value
                         ?? http.User.Identity?.Name
                         ?? "Member";

            var post = new Post
            {
                Content = body.Content.Trim(),
                Title = string.IsNullOrWhiteSpace(body.Title) ? null : body.Title.Trim(),
                Author = author,
                AuthorUserId = userId,
                IsAi = false,
                CreatedAt = DateTime.UtcNow
            };

            db.SavePost(post);
            await notifier.NotifyAsync(FeedEvents.KindPostCreated, post.Id, ct).ConfigureAwait(false);

            try
            {
                await deepThread.GenerateThreadAsync(post.Id, ct).ConfigureAwait(false);
            }
            catch
            {
                // Human post is already durable; deep-thread failures must not roll it back.
            }

            var fresh = db.GetPost(post.Id) ?? post;
            return Results.Created($"/api/posts/{post.Id}", fresh);
        }).RequireAuthorization().RequireRateLimiting("post");

        app.MapPost("/api/posts/{id:guid}/replies", async (
            LiteDbService db,
            ModeratorService moderator,
            IFeedNotifier notifier,
            HttpContext http,
            Guid id,
            CreateReplyBody body,
            CancellationToken ct) =>
        {
            var userId = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();
            if (db.IsUserBanned(userId))
                return Results.Json(new { error = "You are banned from posting." }, statusCode: StatusCodes.Status403Forbidden);
            if (string.IsNullOrWhiteSpace(body.Content))
                return Results.BadRequest("Say something.");
            if (body.Content.Length > 280)
                return Results.BadRequest("280 characters max.");
            if (db.GetPost(id) is null)
                return Results.NotFound();

            var ip = http.Connection.RemoteIpAddress?.ToString();
            var check = moderator.EvaluateAndRecord(body.Content, userId, ip);
            if (!check.Allowed)
                return Results.UnprocessableEntity(check.Reason);

            var author = http.User.FindFirst(ClaimTypes.Name)?.Value
                         ?? http.User.Identity?.Name
                         ?? "Member";

            var reply = new Reply
            {
                Content = body.Content.Trim(),
                Author = author,
                AuthorUserId = userId,
                IsAi = false,
                CreatedAt = DateTime.UtcNow
            };

            var updated = await db.CommitReplyAsync(id, reply, ct).ConfigureAwait(false);
            if (updated is null)
                return Results.NotFound();
            await notifier.NotifyAsync(FeedEvents.KindReplyAdded, id, ct).ConfigureAwait(false);
            return Results.Ok(updated);
        }).RequireAuthorization();

        app.MapPost("/api/posts/{id:guid}/vote", async (
            LiteDbService db,
            IFeedNotifier notifier,
            HttpContext http,
            Guid id,
            VoteBody body,
            CancellationToken ct) =>
        {
            var userId = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();
            if (body.Value is not (-1 or 0 or 1))
                return Results.BadRequest("Value must be -1, 0, or 1.");

            var updated = await db.ApplyPostVoteAsync(userId, id, body.Value, ct).ConfigureAwait(false);
            if (updated is null)
                return Results.NotFound();
            await notifier.NotifyAsync(FeedEvents.KindVoteChanged, id, ct).ConfigureAwait(false);
            return Results.Ok(updated);
        }).RequireAuthorization();

        app.MapPost("/api/posts/{postId:guid}/replies/{replyId:guid}/vote", async (
            LiteDbService db,
            IFeedNotifier notifier,
            HttpContext http,
            Guid postId,
            Guid replyId,
            VoteBody body,
            CancellationToken ct) =>
        {
            var userId = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();
            if (body.Value is not (-1 or 0 or 1))
                return Results.BadRequest("Value must be -1, 0, or 1.");

            var updated = await db.ApplyReplyVoteAsync(userId, postId, replyId, body.Value, ct)
                .ConfigureAwait(false);
            if (updated is null)
                return Results.NotFound();
            await notifier.NotifyAsync(FeedEvents.KindVoteChanged, postId, ct).ConfigureAwait(false);
            return Results.Ok(updated);
        }).RequireAuthorization();

        app.MapPost("/api/posts/{id:guid}/share", async (
            LiteDbService db,
            IFeedNotifier notifier,
            HttpContext http,
            Guid id,
            CancellationToken ct) =>
        {
            if (http.User.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            var updated = await db.UpdatePostLockedAsync(id, p => p.ShareCount++, ct).ConfigureAwait(false);
            if (updated is null)
                return Results.NotFound();

            await notifier.NotifyAsync(FeedEvents.KindShare, id, ct).ConfigureAwait(false);

            var pathBase = (http.Request.PathBase.Value ?? "").TrimEnd('/');
            var path = $"{pathBase}/post/{id}";
            return Results.Ok(new { shareCount = updated.ShareCount, path, url = path });
        }).RequireAuthorization();

        return app;
    }
}
