using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EternalX.Blazor.Server.Hubs;

/// <summary>Anonymous-readable feed push channel (timeline is public read).</summary>
[AllowAnonymous]
public sealed class FeedHub : Hub
{
}
