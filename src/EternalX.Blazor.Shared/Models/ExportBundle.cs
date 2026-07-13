namespace EternalX.Blazor.Shared.Models;

/// <summary>Versioned admin export snapshot (FR-CORE-017).</summary>
public class ExportBundle
{
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
    public List<Post> Posts { get; set; } = new();
    public List<Figure> Figures { get; set; } = new();
    public List<PeerGroup> PeerGroups { get; set; } = new();
    public List<User> Users { get; set; } = new();
    public FeedSettings? Settings { get; set; }
}
