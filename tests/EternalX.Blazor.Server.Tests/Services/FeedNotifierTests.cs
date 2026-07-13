using EternalX.Blazor.Server.Services;
using EternalX.Blazor.Shared;

namespace EternalX.Blazor.Server.Tests.Services;

public class FeedNotifierTests
{
    [Fact]
    public async Task RecordingFeedNotifier_captures_events()
    {
        var n = new RecordingFeedNotifier();
        var id = Guid.NewGuid();
        await n.NotifyAsync(FeedEvents.KindPostCreated, id);
        await n.NotifyAsync(FeedEvents.KindReplyAdded, id);

        Assert.Equal(2, n.Events.Count);
        Assert.Equal(FeedEvents.KindPostCreated, n.Events[0].Kind);
        Assert.Equal(id, n.Events[0].PostId);
        Assert.Equal(FeedEvents.KindReplyAdded, n.Events[1].Kind);
    }

    [Fact]
    public async Task NullFeedNotifier_is_safe()
    {
        var n = new NullFeedNotifier();
        await n.NotifyAsync(FeedEvents.KindFeedCleared);
    }

    [Fact]
    public void FeedEvents_hub_path_is_stable()
    {
        Assert.Equal("/hubs/feed", FeedEvents.HubPath);
        Assert.Equal("FeedChanged", FeedEvents.FeedChanged);
    }
}
