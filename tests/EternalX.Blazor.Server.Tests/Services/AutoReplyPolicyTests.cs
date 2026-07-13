using EternalX.Blazor.Server.Services;
using EternalX.Blazor.Shared.Models;

namespace EternalX.Blazor.Server.Tests.Services;

/// <summary>
/// TEST-BG-001 / FR-BG-001: background auto-reply must not unbounded-loop;
/// quiet period, reply cap, and prompt truncation gates.
/// </summary>
public class AutoReplyPolicyTests
{
    private static readonly AutoReplyOptions Defaults = new();

    private static Post MakePost(
        int replyCount,
        DateTime? lastActivityUtc = null,
        string lastContent = "short prior reply",
        string postContent = "To be, or not to be.")
    {
        var now = lastActivityUtc ?? DateTime.UtcNow;
        var post = new Post
        {
            Content = postContent,
            Author = "human",
            CreatedAt = now.AddHours(-1),
            Replies = new List<Reply>()
        };

        for (var i = 0; i < replyCount; i++)
        {
            var isLast = i == replyCount - 1;
            post.Replies.Add(new Reply
            {
                Content = isLast ? lastContent : $"reply-{i}",
                Author = isLast ? Defaults.AutoReplyAuthor : "Someone",
                CreatedAt = isLast ? now : now.AddMinutes(-(replyCount - i))
            });
        }

        return post;
    }

    [Fact]
    public void IsEligible_false_when_reply_count_at_max()
    {
        var options = Defaults with { MaxRepliesPerPost = 5, MinQuietPeriod = TimeSpan.FromMinutes(2) };
        var post = MakePost(5, DateTime.UtcNow.AddMinutes(-30));

        Assert.False(AutoReplyPolicy.IsEligible(post, DateTime.UtcNow, options));
    }

    [Fact]
    public void IsEligible_false_when_last_activity_inside_quiet_period()
    {
        var options = Defaults with { MaxRepliesPerPost = 50, MinQuietPeriod = TimeSpan.FromMinutes(2) };
        var post = MakePost(1, DateTime.UtcNow.AddSeconds(-30));

        Assert.False(AutoReplyPolicy.IsEligible(post, DateTime.UtcNow, options));
    }

    [Fact]
    public void IsEligible_true_when_quiet_and_under_cap()
    {
        var options = Defaults with { MaxRepliesPerPost = 12, MinQuietPeriod = TimeSpan.FromMinutes(2) };
        var post = MakePost(3, DateTime.UtcNow.AddMinutes(-5));

        Assert.True(AutoReplyPolicy.IsEligible(post, DateTime.UtcNow, options));
    }

    [Fact]
    public void IsEligible_true_for_post_with_no_replies_after_quiet()
    {
        var options = Defaults with { MaxRepliesPerPost = 12, MinQuietPeriod = TimeSpan.FromMinutes(2) };
        var post = new Post
        {
            Content = "seed",
            Author = "human",
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            Replies = new List<Reply>()
        };

        Assert.True(AutoReplyPolicy.IsEligible(post, DateTime.UtcNow, options));
    }

    [Fact]
    public void SelectPostsForTick_respects_max_replies_per_tick()
    {
        var options = Defaults with
        {
            MaxRepliesPerPost = 100,
            MinQuietPeriod = TimeSpan.FromMinutes(1),
            MaxRepliesPerTick = 1
        };
        var now = DateTime.UtcNow;
        var posts = new[]
        {
            MakePost(1, now.AddMinutes(-10), postContent: "a"),
            MakePost(1, now.AddMinutes(-11), postContent: "b"),
            MakePost(1, now.AddMinutes(-12), postContent: "c")
        };

        var selected = AutoReplyPolicy.SelectPostsForTick(posts, now, options);

        Assert.Single(selected);
    }

    [Fact]
    public void SelectPostsForTick_skips_posts_that_already_hit_cap()
    {
        var options = Defaults with
        {
            MaxRepliesPerPost = 3,
            MinQuietPeriod = TimeSpan.FromMinutes(1),
            MaxRepliesPerTick = 5
        };
        var now = DateTime.UtcNow;
        var capped = MakePost(3, now.AddMinutes(-10));
        var open = MakePost(1, now.AddMinutes(-10), postContent: "open");

        var selected = AutoReplyPolicy.SelectPostsForTick(new[] { capped, open }, now, options);

        Assert.Single(selected);
        Assert.Equal("open", selected[0].Content);
    }

    [Fact]
    public void BuildPrompt_truncates_context_to_max_chars()
    {
        var options = Defaults with { MaxContextChars = 40 };
        var huge = new string('x', 500);
        var post = MakePost(1, DateTime.UtcNow.AddMinutes(-10), lastContent: huge);

        var prompt = AutoReplyPolicy.BuildPrompt(post, options);

        Assert.DoesNotContain(huge, prompt);
        Assert.Contains("...", prompt);
        // Prompt should remain bounded well under unbounded growth (565-reply failure mode).
        Assert.True(prompt.Length < 200, $"prompt length {prompt.Length}");
    }

    [Fact]
    public void BuildPrompt_uses_post_content_when_no_replies()
    {
        var post = new Post { Content = "Hamlet soliloquy", Replies = new List<Reply>() };
        var prompt = AutoReplyPolicy.BuildPrompt(post, Defaults);

        Assert.Contains("Hamlet soliloquy", prompt);
    }

    [Fact]
    public void Simulated_ticks_do_not_exceed_max_replies_per_post()
    {
        // Models the production failure: repeated ticks must stop once the cap is hit.
        var options = Defaults with
        {
            MaxRepliesPerPost = 7,
            MinQuietPeriod = TimeSpan.Zero,
            MaxRepliesPerTick = 1
        };
        var post = MakePost(0, DateTime.UtcNow.AddHours(-1));
        var now = DateTime.UtcNow;

        for (var tick = 0; tick < 50; tick++)
        {
            if (!AutoReplyPolicy.IsEligible(post, now, options))
                break;

            post.Replies.Add(new Reply
            {
                Content = AutoReplyPolicy.BuildPrompt(post, options),
                Author = options.AutoReplyAuthor,
                CreatedAt = now
            });
            now = now.AddSeconds(10);
        }

        Assert.Equal(options.MaxRepliesPerPost, post.Replies.Count);
        Assert.False(AutoReplyPolicy.IsEligible(post, now, options));
    }
}
