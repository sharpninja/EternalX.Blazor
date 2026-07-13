using EternalX.Blazor.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace EternalX.Blazor.Client.Services;

/// <summary>Blazor WASM client for <see cref="FeedEvents"/> SignalR hub.</summary>
public sealed class FeedRealtimeService : IAsyncDisposable
{
    private readonly NavigationManager _nav;
    private HubConnection? _connection;
    private int _startAttempts;

    public FeedRealtimeService(NavigationManager nav) => _nav = nav;

    public event Func<FeedChangeMessage, Task>? FeedChanged;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public async Task StartAsync()
    {
        if (_connection is not null)
            return;

        var hubUrl = _nav.ToAbsoluteUri(FeedEvents.HubPath.TrimStart('/')).ToString();
        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect(new[]
            {
                TimeSpan.Zero,
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(30)
            })
            .Build();

        _connection.On<FeedChangeMessage>(FeedEvents.FeedChanged, async msg =>
        {
            var handlers = FeedChanged;
            if (handlers is not null)
                await handlers.Invoke(msg);
        });

        try
        {
            await _connection.StartAsync();
            _startAttempts = 0;
        }
        catch
        {
            _startAttempts++;
            // Leave connection for retry; caller may fall back to polling.
        }
    }

    public async Task EnsureConnectedAsync()
    {
        if (_connection is null)
        {
            await StartAsync();
            return;
        }

        if (_connection.State == HubConnectionState.Disconnected && _startAttempts < 5)
        {
            try
            {
                await _connection.StartAsync();
                _startAttempts = 0;
            }
            catch
            {
                _startAttempts++;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            try { await _connection.DisposeAsync(); } catch { }
            _connection = null;
        }
    }
}
