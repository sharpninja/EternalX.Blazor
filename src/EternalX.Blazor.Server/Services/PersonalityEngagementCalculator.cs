using EternalX.Blazor.Shared;
using EternalX.Blazor.Shared.Models;

namespace EternalX.Blazor.Server.Services;

/// <summary>
/// Pure aggregation of X-style engagement by historical personality (figure).
/// Unit-tested without LiteDB.
/// </summary>
public static class PersonalityEngagementCalculator
{
    public static IReadOnlyList<PersonalityEngagement> Calculate(
        IReadOnlyList<Figure> figures,
        IReadOnlyList<Post> posts)
    {
        ArgumentNullException.ThrowIfNull(figures);
        ArgumentNullException.ThrowIfNull(posts);

        // Pre-index posts by id for quote targets.
        var byId = posts.ToDictionary(p => p.Id);

        // Mentions of figure names across all content (posts + replies + quote comments).
        var mentionHits = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var post in posts)
        {
            CountMentions(post.Content, mentionHits);
            CountMentions(post.Mentions, mentionHits);
            foreach (var reply in post.Replies ?? Enumerable.Empty<Reply>())
            {
                CountMentions(reply.Content, mentionHits);
                CountMentions(reply.Mentions, mentionHits);
            }
        }

        var results = new List<PersonalityEngagement>(figures.Count);
        foreach (var fig in figures)
        {
            var postsAuthored = 0;
            var repliesAuthored = 0;
            var likes = 0;
            var reshares = 0;
            var repliesReceived = 0;

            foreach (var post in posts)
            {
                if (post.IsAi && string.Equals(post.FigureId, fig.Id, StringComparison.OrdinalIgnoreCase))
                {
                    postsAuthored++;
                    likes += post.LikeCount;
                    reshares += post.ReshareCount;
                    repliesReceived += post.Replies?.Count ?? 0;
                }

                foreach (var reply in post.Replies ?? Enumerable.Empty<Reply>())
                {
                    if (reply.IsAi && string.Equals(reply.FigureId, fig.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        repliesAuthored++;
                        likes += reply.LikeCount;
                    }
                }
            }

            // Also count quote-reshares that point at this figure's posts (in case ReshareCount lagged).
            var quoted = posts.Count(p =>
                p.QuotedPostId is Guid qid &&
                byId.TryGetValue(qid, out var src) &&
                src.IsAi &&
                string.Equals(src.FigureId, fig.Id, StringComparison.OrdinalIgnoreCase));
            if (quoted > reshares)
                reshares = quoted;

            var handle = FigureHandles.ResolveUsername(fig.Username, fig.Name);
            mentionHits.TryGetValue(handle, out var mentions);
            // Also match historical name squeezed as handle (legacy mentions)
            if (mentionHits.TryGetValue(ToHandle(fig.Name), out var m2))
                mentions = Math.Max(mentions, m2);
            if (mentionHits.TryGetValue(fig.Name, out var m3))
                mentions = Math.Max(mentions, m3);

            var score = likes + (2 * reshares) + repliesReceived + mentions;

            results.Add(new PersonalityEngagement
            {
                FigureId = fig.Id,
                Name = fig.Name,
                Username = handle,
                Enabled = fig.Enabled,
                PeerGroupIds = fig.PeerGroupIds?.ToList() ?? new List<string>(),
                PostsAuthored = postsAuthored,
                RepliesAuthored = repliesAuthored,
                LikesReceived = likes,
                ResharesReceived = reshares,
                RepliesReceived = repliesReceived,
                MentionsReceived = mentions,
                EngagementScore = score
            });
        }

        return results
            .OrderByDescending(r => r.EngagementScore)
            .ThenByDescending(r => r.LikesReceived)
            .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void CountMentions(string? content, Dictionary<string, int> hits)
    {
        if (string.IsNullOrEmpty(content))
            return;
        foreach (var m in ContentTags.ExtractMentions(content))
        {
            hits.TryGetValue(m, out var n);
            hits[m] = n + 1;
        }
    }

    private static void CountMentions(IEnumerable<string>? mentions, Dictionary<string, int> hits)
    {
        if (mentions is null)
            return;
        foreach (var m in mentions)
        {
            if (string.IsNullOrWhiteSpace(m))
                continue;
            hits.TryGetValue(m, out var n);
            hits[m] = n + 1;
        }
    }

    /// <summary>Normalize "Ada Lovelace" → "AdaLovelace" for @handle comparison.</summary>
    public static string ToHandle(string name)
        => string.Concat((name ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
