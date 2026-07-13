using EternalX.Blazor.Server.Data;
using EternalX.Blazor.Shared;
using EternalX.Blazor.Shared.Models;

namespace EternalX.Blazor.Server.Services;

/// <summary>
/// Background auto-reply tick (FR-AI-013 / FR-AI-002). Uses <see cref="AutoReplyPolicy"/>,
/// roster figure picks, and feed pause settings.
/// </summary>
public class AutoReplyBackgroundService : BackgroundService
{
    private readonly LiteDbService _db;
    private readonly AiService _ai;
    private readonly ModeratorService _moderator;
    private readonly AutoReplyOptions _options;
    private readonly IFeedNotifier _notifier;
    private readonly ILogger<AutoReplyBackgroundService> _logger;

    public AutoReplyBackgroundService(
        LiteDbService db,
        AiService ai,
        ModeratorService moderator,
        AutoReplyOptions options,
        IFeedNotifier notifier,
        ILogger<AutoReplyBackgroundService> logger)
    {
        _db = db;
        _ai = ai;
        _moderator = moderator;
        _options = options;
        _notifier = notifier;
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
        var settings = _db.GetSettings();
        if (settings.AutoReplyPaused)
        {
            _logger.LogDebug("Auto-reply paused; skipping tick");
            return;
        }

        var now = DateTime.UtcNow;
        var recent = _db.GetRecentPosts(_options.MaxPostsToScan).ToList();

        if (ShouldCreateIdlePost(recent, now))
        {
            await CreateIdlePostAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        var selected = AutoReplyPolicy.SelectPostsForTick(recent, now, _options);

        foreach (var post in selected)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var latest = _db.GetPost(post.Id) ?? post;
            if (!AutoReplyPolicy.IsEligible(latest, DateTime.UtcNow, _options))
                continue;

            var roster = _db.GetFigures(enabledOnly: true);
            var recentIds = (latest.Replies ?? [])
                .Where(r => !string.IsNullOrEmpty(r.FigureId))
                .Select(r => r.FigureId!)
                .TakeLast(5)
                .ToList();
            var figure = FigurePicker.Pick(roster, recentFigureIds: recentIds);
            if (figure is null)
                continue;

            var prompt = AutoReplyPolicy.BuildPrompt(latest, _options);
            AiResult result;
            try
            {
                result = await _ai.GenerateForFigureAsync(figure, prompt, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auto-reply generation failed for post {PostId}", latest.Id);
                continue;
            }

            var check = _moderator.CheckContent(result.Text);
            if (!check.Allowed)
            {
                _logger.LogInformation(
                    "Auto-reply blocked by moderator for post {PostId}: {Reason}",
                    latest.Id,
                    check.Reason);
                continue;
            }

            var reply = new Reply
            {
                Id = Guid.NewGuid(),
                Content = result.Text,
                Author = figure.Name,
                IsAi = true,
                FigureId = figure.Id,
                Provider = result.Provider,
                Model = result.Model,
                CreatedAt = DateTime.UtcNow
            };

            await _db.CommitReplyAsync(latest.Id, reply, cancellationToken).ConfigureAwait(false);
            await _notifier.NotifyAsync(FeedEvents.KindReplyAdded, latest.Id, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Auto-reply added to post {PostId} by {Figure}; provider={Provider}",
                latest.Id,
                figure.Name,
                result.Provider);
        }
    }

    private static bool ShouldCreateIdlePost(IReadOnlyList<Post> recent, DateTime utcNow)
    {
        if (recent.Count == 0)
            return true;

        var last = recent.Max(p => AutoReplyPolicy.GetLastActivityUtc(p));
        return utcNow - last >= TimeSpan.FromHours(1);
    }

    private async Task CreateIdlePostAsync(CancellationToken cancellationToken)
    {
        var roster = _db.GetFigures(enabledOnly: true);
        var figure = FigurePicker.Pick(roster);
        if (figure is null)
            return;

        var prompt = "Share a brief, original observation or question for the timeline. Stay in character.";
        AiResult result;
        try
        {
            result = await _ai.GenerateForFigureAsync(figure, prompt, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Idle original post generation failed");
            return;
        }

        if (!_moderator.CheckContent(result.Text).Allowed)
            return;

        var idle = new Post
        {
            Content = result.Text,
            Author = figure.Name,
            IsAi = true,
            FigureId = figure.Id,
            Provider = result.Provider,
            Model = result.Model,
            CreatedAt = DateTime.UtcNow
        };
        _db.SavePost(idle);
        await _notifier.NotifyAsync(FeedEvents.KindPostCreated, idle.Id, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("Idle original post by {Figure}", figure.Name);
    }
}
