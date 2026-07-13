using EternalX.Blazor.Shared;

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
}
