using EternalX.Blazor.Server.Data;
using EternalX.Blazor.Shared.Models;

namespace EternalX.Blazor.Server.Services;

public class AutoReplyBackgroundService : BackgroundService
{
    private readonly LiteDbService _db;
    private readonly AiService _ai;
    private readonly ModeratorService _moderator;

    public AutoReplyBackgroundService(LiteDbService db, AiService ai, ModeratorService moderator)
    {
        _db = db;
        _ai = ai;
        _moderator = moderator;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Every 10 seconds: find recent threads and add an interesting AI reply
                var recentPosts = _db.GetRecentPosts(20).ToList();

                foreach (var post in recentPosts.Take(3))
                {
                    if (post.Replies?.Count > 0)
                    {
                        var lastReply = post.Replies.Last();
                        var newReply = await _ai.GenerateReplyAsync($"Continue this historical discussion: {lastReply.Content}");

                        var check = _moderator.CheckContent(newReply);
                        if (check.IsSafe)
                        {
                            post.Replies.Add(new Reply 
                            { 
                                Id = Guid.NewGuid(), 
                                Content = newReply, 
                                Author = "AutoModerator AI", 
                                CreatedAt = DateTime.UtcNow 
                            });
                            _db.UpdatePost(post);
                        }
                    }
                }
            }
            catch { /* log error */ }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}