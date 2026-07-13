using System.Security.Claims;
using EternalX.Blazor.Server.Data;
using EternalX.Blazor.Server.Services;
using EternalX.Blazor.Shared.Models;

namespace EternalX.Blazor.Server.Api;

/// <summary>
/// Minimal API surface for the client (the frontend the requirements list as
/// pending): read the feed, post (moderated + rate limited), and auth state.
/// Uses the existing LiteDbService / ModeratorService / AiService as-is.
/// </summary>
public static class PostEndpoints
{
    public sealed record CreatePostBody(string Content);

    public static IEndpointRouteBuilder MapPostEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/posts", (LiteDbService db, int? count) =>
            Results.Ok(db.GetRecentPosts(count is > 0 ? count.Value : 50)));

        app.MapGet("/api/me", (HttpContext http) =>
        {
            var authed = http.User.Identity?.IsAuthenticated ?? false;
            return Results.Ok(new
            {
                authenticated = authed,
                name = authed ? http.User.FindFirst(ClaimTypes.Name)?.Value ?? http.User.Identity?.Name : null,
                gateway = true
            });
        });

        app.MapPost("/api/posts", async (LiteDbService db, ModeratorService moderator, AiService ai,
            HttpContext http, CreatePostBody body, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.Content)) return Results.BadRequest("Say something.");
            if (body.Content.Length > 280) return Results.BadRequest("280 characters max.");

            var check = moderator.CheckContent(body.Content);
            if (!check.IsSafe) return Results.UnprocessableEntity(check.Reason);

            var author = http.User.FindFirst(ClaimTypes.Name)?.Value ?? http.User.Identity?.Name ?? "Member";
            var post = new Post { Content = body.Content.Trim(), Author = author };

            // Seed the first AI reply so the background service has a thread to grow.
            var reply = await ai.GenerateReplyAsync(post.Content, "claude");
            if (moderator.CheckContent(reply).IsSafe)
                post.Replies.Add(new Reply { Content = reply, Author = "Historical AI" });

            db.SavePost(post);
            return Results.Created($"/api/posts/{post.Id}", post);
        }).RequireAuthorization().RequireRateLimiting("post");

        return app;
    }
}
