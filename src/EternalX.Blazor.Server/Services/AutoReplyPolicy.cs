using EternalX.Blazor.Shared.Models;

namespace EternalX.Blazor.Server.Services;

/// <summary>
/// Pure selection/prompt rules for <see cref="AutoReplyBackgroundService"/>.
/// Unit-tested independently of the hosted service loop.
/// </summary>
public static class AutoReplyPolicy
{
    public static DateTime GetLastActivityUtc(Post post)
    {
        ArgumentNullException.ThrowIfNull(post);

        var last = post.CreatedAt;
        if (post.Replies is { Count: > 0 })
        {
            foreach (var reply in post.Replies)
            {
                if (reply.CreatedAt > last)
                    last = reply.CreatedAt;
            }
        }

        return last.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(last, DateTimeKind.Utc)
            : last.ToUniversalTime();
    }

    public static bool IsEligible(Post post, DateTime utcNow, AutoReplyOptions options)
    {
        ArgumentNullException.ThrowIfNull(post);
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(post.Content) && (post.Replies is null || post.Replies.Count == 0))
            return false;

        var replyCount = post.Replies?.Count ?? 0;
        if (replyCount >= options.MaxRepliesPerPost)
            return false;

        var lastActivity = GetLastActivityUtc(post);
        var quiet = utcNow.ToUniversalTime() - lastActivity;
        if (quiet < options.MinQuietPeriod)
            return false;

        return true;
    }

    public static IReadOnlyList<Post> SelectPostsForTick(
        IEnumerable<Post> recentPosts,
        DateTime utcNow,
        AutoReplyOptions options)
    {
        ArgumentNullException.ThrowIfNull(recentPosts);
        ArgumentNullException.ThrowIfNull(options);

        return recentPosts
            .Where(p => IsEligible(p, utcNow, options))
            .Take(Math.Max(0, options.MaxRepliesPerTick))
            .ToList();
    }

    public static string BuildPrompt(Post post, AutoReplyOptions options)
    {
        ArgumentNullException.ThrowIfNull(post);
        ArgumentNullException.ThrowIfNull(options);

        string context;
        if (post.Replies is { Count: > 0 })
            context = post.Replies[^1].Content ?? string.Empty;
        else
            context = post.Content ?? string.Empty;

        context = Truncate(context.Trim(), options.MaxContextChars);
        return $"Continue this historical discussion briefly and in character. Context: {context}";
    }

    public static string Truncate(string value, int maxChars)
    {
        if (string.IsNullOrEmpty(value) || maxChars <= 0)
            return string.Empty;

        if (value.Length <= maxChars)
            return value;

        if (maxChars <= 3)
            return value[..maxChars];

        return value[..(maxChars - 3)] + "...";
    }
}
