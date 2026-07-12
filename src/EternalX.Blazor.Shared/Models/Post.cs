namespace EternalX.Blazor.Shared.Models;

public class Post
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Content { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Reply> Replies { get; set; } = new();
    public int Upvotes { get; set; }
    public int Downvotes { get; set; }
}

public class Reply
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Content { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int Upvotes { get; set; }
    public int Downvotes { get; set; }
}