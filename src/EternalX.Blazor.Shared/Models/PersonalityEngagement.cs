namespace EternalX.Blazor.Shared.Models;

/// <summary>
/// Engagement rollup for one historical personality (figure) on the X-style feed.
/// </summary>
public sealed class PersonalityEngagement
{
    public string FigureId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    /// <summary>Self-picked @handle without leading @.</summary>
    public string Username { get; init; } = string.Empty;
    public bool Enabled { get; init; }
    public IReadOnlyList<string> PeerGroupIds { get; init; } = Array.Empty<string>();

    /// <summary>AI posts authored by this figure (top-level).</summary>
    public int PostsAuthored { get; init; }

    /// <summary>AI replies authored by this figure.</summary>
    public int RepliesAuthored { get; init; }

    /// <summary>Hearts on this figure's posts + replies.</summary>
    public int LikesReceived { get; init; }

    /// <summary>Quote-reshares of this figure's posts.</summary>
    public int ResharesReceived { get; init; }

    /// <summary>Human/AI replies under this figure's posts (conversation depth).</summary>
    public int RepliesReceived { get; init; }

    /// <summary>@mentions of this figure's username (or name fallback) across the feed.</summary>
    public int MentionsReceived { get; init; }

    /// <summary>
    /// Weighted score: likes + 2×reshares + replies received + mentions.
    /// Used to rank personalities by engagement.
    /// </summary>
    public int EngagementScore { get; init; }
}
