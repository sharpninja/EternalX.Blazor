namespace EternalX.Blazor.Shared;

/// <summary>SignalR contract for near-real-time timeline updates (FR-UI-002).</summary>
public static class FeedEvents
{
    public const string HubPath = "/hubs/feed";
    public const string FeedChanged = "FeedChanged";

    public const string KindPostCreated = "post-created";
    public const string KindReplyAdded = "reply-added";
    public const string KindVoteChanged = "vote-changed";
    public const string KindShare = "share";
    public const string KindFeedCleared = "feed-cleared";
    public const string KindSettings = "settings";
}

public sealed record FeedChangeMessage(string Kind, Guid? PostId, DateTime Utc);
