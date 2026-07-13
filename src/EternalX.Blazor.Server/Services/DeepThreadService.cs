using EternalX.Blazor.Server.Data;
using EternalX.Blazor.Shared;
using EternalX.Blazor.Shared.Models;

namespace EternalX.Blazor.Server.Services;

/// <summary>
/// Generates a bounded deep reply thread after a user post (FR-AI-003, TR-AI-THREAD-001).
/// </summary>
public class DeepThreadService
{
    public const int DefaultMinReplies = 5;
    public const int DefaultMaxReplies = 7;

    private readonly LiteDbService _db;
    private readonly AiService _ai;
    private readonly ModeratorService _moderator;
    private readonly IFeedNotifier _notifier;
    private readonly ILogger<DeepThreadService> _logger;
    private readonly int _min;
    private readonly int _max;

    public DeepThreadService(
        LiteDbService db,
        AiService ai,
        ModeratorService moderator,
        IFeedNotifier notifier,
        ILogger<DeepThreadService> logger,
        int minReplies = DefaultMinReplies,
        int maxReplies = DefaultMaxReplies)
    {
        _db = db;
        _ai = ai;
        _moderator = moderator;
        _notifier = notifier;
        _logger = logger;
        _min = Math.Max(1, minReplies);
        _max = Math.Max(_min, maxReplies);
    }

    public async Task<int> GenerateThreadAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        var post = _db.GetPost(postId);
        if (post is null)
            return 0;

        var target = Random.Shared.Next(_min, _max + 1);
        var recentFigureIds = new List<string>();
        var added = 0;

        for (var i = 0; i < target; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var roster = _db.GetFigures(enabledOnly: true);
            var figure = FigurePicker.Pick(roster, recentFigureIds: recentFigureIds);
            if (figure is null)
            {
                _logger.LogWarning("No enabled figures available for deep thread on {PostId}", postId);
                break;
            }

            var context = BuildContext(post, maxChars: 500);
            var prompt =
                $"The human posted: {post.Content}\n" +
                $"Thread so far:\n{context}\n" +
                "Reply briefly in character, interacting with the discussion.";

            AiResult result;
            try
            {
                result = await _ai.GenerateRoundRobinAsync(figure, prompt, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Deep thread generation failed for post {PostId}", postId);
                continue;
            }

            var check = _moderator.CheckContent(result.Text);
            if (!check.Allowed)
            {
                _logger.LogInformation(
                    "Deep thread reply blocked for post {PostId}: {Reason}",
                    postId,
                    check.Reason);
                continue;
            }

            var reply = new Reply
            {
                Content = result.Text,
                Author = figure.Name,
                IsAi = true,
                FigureId = figure.Id,
                Provider = result.Provider,
                Model = result.Model,
                CreatedAt = DateTime.UtcNow
            };

            await _db.CommitReplyAsync(postId, reply, cancellationToken).ConfigureAwait(false);
            await _notifier.NotifyAsync(FeedEvents.KindReplyAdded, postId, cancellationToken)
                .ConfigureAwait(false);

            recentFigureIds.Add(figure.Id);
            if (recentFigureIds.Count > 5)
                recentFigureIds.RemoveAt(0);

            post = _db.GetPost(postId) ?? post;
            added++;
        }

        _logger.LogInformation("Deep thread added {Count} replies to post {PostId}", added, postId);
        return added;
    }

    private static string BuildContext(Post post, int maxChars)
    {
        var parts = new List<string> { $"{post.Author}: {post.Content}" };
        foreach (var r in post.Replies ?? Enumerable.Empty<Reply>())
            parts.Add($"{r.Author}: {r.Content}");

        var joined = string.Join("\n", parts);
        return AutoReplyPolicy.Truncate(joined, maxChars);
    }
}
