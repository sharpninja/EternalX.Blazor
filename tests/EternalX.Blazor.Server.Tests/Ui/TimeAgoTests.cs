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

    [Fact]
    public void Score_is_up_minus_down()
    {
        Assert.Equal(3, FeedFormatting.Score(5, 2));
        Assert.Equal(-1, FeedFormatting.Score(0, 1));
    }
}
