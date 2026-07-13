namespace EternalX.Blazor.Server.Services;

/// <summary>
/// Bounds for background auto-reply (FR-BG-001). Defaults stop unbounded continue-chains
/// observed in production (565+ replies on a single post).
/// </summary>
public sealed record AutoReplyOptions
{
    /// <summary>Hosted service delay between ticks.</summary>
    public TimeSpan TickInterval { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>Minimum silence since last post/reply activity before another auto-reply.</summary>
    public TimeSpan MinQuietPeriod { get; init; } = TimeSpan.FromMinutes(2);

    /// <summary>Hard cap on total replies per post (deep thread + background).</summary>
    public int MaxRepliesPerPost { get; init; } = 12;

    /// <summary>How many recent posts to scan per tick.</summary>
    public int MaxPostsToScan { get; init; } = 20;

    /// <summary>Maximum posts that receive an auto-reply in a single tick.</summary>
    public int MaxRepliesPerTick { get; init; } = 1;

    /// <summary>Max characters of prior content embedded in the generation prompt.</summary>
    public int MaxContextChars { get; init; } = 500;

    /// <summary>Author stamp written on auto-generated replies.</summary>
    public string AutoReplyAuthor { get; init; } = "AutoReply AI";
}
