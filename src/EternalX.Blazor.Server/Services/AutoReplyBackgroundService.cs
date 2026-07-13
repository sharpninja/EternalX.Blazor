using EternalX.Blazor.Server.Data;
using EternalX.Blazor.Shared.Models;

namespace EternalX.Blazor.Server.Services;

/// <summary>
/// Background auto-reply tick (FR-BG-001). Uses <see cref="AutoReplyPolicy"/> so
/// quiet period, per-post reply caps, and prompt truncation prevent unbounded chains.
/// </summary>
public class AutoReplyBackgroundService : BackgroundService
{
    private readonly LiteDbService _db;
    private readonly AiService _ai;
    private readonly ModeratorService _moderator;
    private readonly AutoReplyOptions _options;
    private readonly ILogger<AutoReplyBackgroundService> _logger;

    public AutoReplyBackgroundService(
        LiteDbService db,
        AiService ai,
        ModeratorService moderator,
        AutoReplyOptions options,
        ILogger<AutoReplyBackgroundService> logger)
    {
        _db = db;
        _ai = ai;
        _moderator = moderator;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auto-reply tick failed");
            }

            try
            {
                await Task.Delay(_options.TickInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    internal async Task TickAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var recent = _db.GetRecentPosts(_options.MaxPostsToScan).ToList();
        var selected = AutoReplyPolicy.SelectPostsForTick(recent, now, _options);

        foreach (var post in selected)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Re-check against latest state after prior updates in this tick.
            if (!AutoReplyPolicy.IsEligible(post, DateTime.UtcNow, _options))
                continue;

            var prompt = AutoReplyPolicy.BuildPrompt(post, _options);
            var newReplyBody = await _ai.GenerateReplyAsync(prompt).ConfigureAwait(false);

            var check = _moderator.CheckContent(newReplyBody);
            if (!check.IsSafe)
            {
                _logger.LogInformation(
                    "Auto-reply blocked by moderator for post {PostId}: {Reason}",
                    post.Id,
                    check.Reason);
                continue;
            }

            post.Replies ??= new List<Reply>();
            post.Replies.Add(new Reply
            {
                Id = Guid.NewGuid(),
                Content = newReplyBody,
                Author = _options.AutoReplyAuthor,
                CreatedAt = DateTime.UtcNow
            });
            _db.UpdatePost(post);

            _logger.LogInformation(
                "Auto-reply added to post {PostId}; replyCount={ReplyCount}",
                post.Id,
                post.Replies.Count);
        }
    }
}
