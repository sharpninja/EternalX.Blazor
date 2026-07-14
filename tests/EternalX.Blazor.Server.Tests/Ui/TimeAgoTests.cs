using EternalX.Blazor.Shared;
using EternalX.Blazor.Shared.Models;

namespace EternalX.Blazor.Server.Tests.Ui;

/// <summary>TEST-UI-001 pure UI helper coverage.</summary>
public class TimeAgoTests
{
    [Fact]
    public void Formats_minutes_hours_days()
    {
        var now = new DateTime(2026, 7, 13, 12, 0, 0, DateTimeKind.Utc);
        Assert.Equal("1m", FeedFormatting.Ago(now.AddMinutes(-1), now));
        Assert.Equal("3h", FeedFormatting.Ago(now.AddHours(-3), now));
        Assert.Equal("2d", FeedFormatting.Ago(now.AddDays(-2), now));
    }

    [Fact]
    public void Author_helpers_prefer_username_not_formal_name()
    {
        Assert.Equal("GadflyAthens", FeedFormatting.Handle("@GadflyAthens"));
        Assert.Equal("@GadflyAthens", FeedFormatting.AtHandle("@GadflyAthens"));
        Assert.Equal("GadflyAthens", FeedFormatting.DisplayName("@GadflyAthens"));
        Assert.Equal("GA", FeedFormatting.Initials("@GadflyAthens"));
        Assert.Equal("Socrates", FeedFormatting.Handle("Socrates"));
        Assert.Equal("", FeedFormatting.CountLabel(0));
        Assert.Equal("12", FeedFormatting.CountLabel(12));
        Assert.Equal("1.2K", FeedFormatting.FormatCount(1200));
    }

    [Fact]
    public void Meta_line_shows_real_name_next_to_username()
    {
        Assert.Equal("Socrates", FeedFormatting.MetaDisplayName("Socrates", "GadflyAthens"));
        Assert.Equal("@GadflyAthens", FeedFormatting.MetaAtHandle("Socrates", "GadflyAthens"));
        // Legacy rows that only stored @handle in Author still render both slots.
        Assert.Equal("GadflyAthens", FeedFormatting.MetaDisplayName("@GadflyAthens", null));
        Assert.Equal("@GadflyAthens", FeedFormatting.MetaAtHandle("@GadflyAthens", null));
    }
}

public class ContentTagsTests
{
    [Fact]
    public void Extracts_mentions_and_hashtags()
    {
        var text = "Hello @Ada_Lovelace and @socrates about #math and #History!";
        var mentions = ContentTags.ExtractMentions(text);
        var tags = ContentTags.ExtractHashtags(text);

        Assert.Contains("Ada_Lovelace", mentions);
        Assert.Contains("socrates", mentions);
        Assert.Contains("math", tags);
        Assert.Contains("History", tags);
    }

    [Fact]
    public void Tokenize_splits_segments()
    {
        var segs = ContentTags.Tokenize("See @Hypatia on #astronomy today");
        Assert.Contains(segs, s => s.Kind == ContentTags.SegmentKind.Mention && s.Text == "@Hypatia");
        Assert.Contains(segs, s => s.Kind == ContentTags.SegmentKind.Hashtag && s.Text == "#astronomy");
        Assert.Contains(segs, s => s.Kind == ContentTags.SegmentKind.Text && s.Text.Contains("See"));
    }

    [Fact]
    public void NormalizeHashtag_strips_hash_and_junk()
    {
        Assert.Equal("math", ContentTags.NormalizeHashtag("#math"));
        Assert.Equal("History", ContentTags.NormalizeHashtag("History"));
        Assert.Equal("a_b1", ContentTags.NormalizeHashtag("#a_b1!"));
        Assert.Equal("", ContentTags.NormalizeHashtag("###"));
    }

    [Fact]
    public void FilterByHashtag_matches_post_reply_and_stored_tags()
    {
        var matchPost = new Post
        {
            Id = Guid.NewGuid(),
            Content = "Talking #Math today",
            Author = "a",
            CreatedAt = DateTime.UtcNow.AddMinutes(-2),
            Hashtags = new List<string> { "Math" }
        };
        var matchReply = new Post
        {
            Id = Guid.NewGuid(),
            Content = "No tag here",
            Author = "b",
            CreatedAt = DateTime.UtcNow.AddMinutes(-1),
            Replies =
            [
                new Reply { Content = "But reply has #math", Author = "c" }
            ]
        };
        var other = new Post
        {
            Id = Guid.NewGuid(),
            Content = "Just #history",
            Author = "d",
            CreatedAt = DateTime.UtcNow,
            Hashtags = new List<string> { "history" }
        };

        var filtered = ContentTags.FilterByHashtag(new[] { matchPost, matchReply, other }, "math");
        Assert.Equal(2, filtered.Count);
        Assert.Equal(matchReply.Id, filtered[0].Id); // newest first among matches
        Assert.Equal(matchPost.Id, filtered[1].Id);
        Assert.DoesNotContain(filtered, p => p.Id == other.Id);
    }
}
