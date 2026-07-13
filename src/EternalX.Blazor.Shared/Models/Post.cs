namespace EternalX.Blazor.Shared.Models;

public class Post
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Title { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? AuthorUserId { get; set; }
    public bool IsAi { get; set; }
    public string? FigureId { get; set; }
    public string? Provider { get; set; }
    public string? Model { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Reply> Replies { get; set; } = new();

    /// <summary>X-style likes (hearts). Stored historically as Upvotes in LiteDB.</summary>
    public int LikeCount
    {
        get => Upvotes;
        set => Upvotes = value;
    }

    /// <summary>Legacy field name kept for LiteDB document compatibility.</summary>
    public int Upvotes { get; set; }

    /// <summary>Deprecated; X does not use downvotes. Retained for old documents.</summary>
    public int Downvotes { get; set; }

    /// <summary>Quote-reshare count (new posts that quote this one).</summary>
    public int ReshareCount
    {
        get => ShareCount;
        set => ShareCount = value;
    }

    /// <summary>Legacy field name kept for LiteDB document compatibility.</summary>
    public int ShareCount { get; set; }

    /// <summary>When this post is a quote-reshare of another post.</summary>
    public Guid? QuotedPostId { get; set; }
    public string? QuotedAuthor { get; set; }
    public string? QuotedContent { get; set; }
    public bool QuotedIsAi { get; set; }

    /// <summary>@handles referenced in content (without @).</summary>
    public List<string> Mentions { get; set; } = new();

    /// <summary>#topics referenced in content (without #).</summary>
    public List<string> Hashtags { get; set; } = new();
}

public class Reply
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Content { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? AuthorUserId { get; set; }
    public bool IsAi { get; set; }
    public string? FigureId { get; set; }
    public string? Provider { get; set; }
    public string? Model { get; set; }
    public Guid? ParentReplyId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int LikeCount
    {
        get => Upvotes;
        set => Upvotes = value;
    }

    public int Upvotes { get; set; }
    public int Downvotes { get; set; }

    public List<string> Mentions { get; set; } = new();
    public List<string> Hashtags { get; set; } = new();
}
