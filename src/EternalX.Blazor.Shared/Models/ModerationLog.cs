namespace EternalX.Blazor.Shared.Models;

public class ModerationLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool Allowed { get; set; }
    public bool IsInjection { get; set; }
    public bool IsNsfw { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ContentExcerpt { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? Ip { get; set; }
}
