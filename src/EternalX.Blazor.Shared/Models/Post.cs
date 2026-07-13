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
    public int Upvotes { get; set; }
    public int Downvotes { get; set; }
    public int ShareCount { get; set; }
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
    public int Upvotes { get; set; }
    public int Downvotes { get; set; }
}
