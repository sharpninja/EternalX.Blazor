using EternalX.Blazor.Shared;
using Microsoft.AspNetCore.SignalR;

namespace EternalX.Blazor.Server.Services;

public interface IFeedNotifier
{
    Task NotifyAsync(string kind, Guid? postId = null, CancellationToken cancellationToken = default);
}

/// <summary>Broadcasts <see cref="FeedEvents.FeedChanged"/> to all connected clients.</summary>
public sealed class SignalRFeedNotifier : IFeedNotifier
{
    private readonly IHubContext<Hubs.FeedHub> _hub;
    private readonly ILogger<SignalRFeedNotifier> _logger;

    public SignalRFeedNotifier(IHubContext<Hubs.FeedHub> hub, ILogger<SignalRFeedNotifier> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task NotifyAsync(string kind, Guid? postId = null, CancellationToken cancellationToken = default)
    {
        var message = new FeedChangeMessage(kind, postId, DateTime.UtcNow);
        try
        {
            await _hub.Clients.All.SendAsync(FeedEvents.FeedChanged, message, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Feed notify failed kind={Kind} postId={PostId}", kind, postId);
        }
    }
}

/// <summary>No-op notifier for unit tests that do not host SignalR.</summary>
public sealed class NullFeedNotifier : IFeedNotifier
{
    public Task NotifyAsync(string kind, Guid? postId = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

/// <summary>Records notifications for unit tests.</summary>
public sealed class RecordingFeedNotifier : IFeedNotifier
{
    public List<(string Kind, Guid? PostId)> Events { get; } = new();

    public Task NotifyAsync(string kind, Guid? postId = null, CancellationToken cancellationToken = default)
    {
        Events.Add((kind, postId));
        return Task.CompletedTask;
    }
}
