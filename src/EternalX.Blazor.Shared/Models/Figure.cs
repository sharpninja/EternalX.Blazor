namespace EternalX.Blazor.Shared.Models;

public class Figure
{
    public string Id { get; set; } = string.Empty;

    /// <summary>Historical display name (admin/roster only; not used as feed author).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Self-picked X-style handle without leading @. Used as the timeline author
    /// for AI posts and replies (shown as @Username).
    /// </summary>
    public string Username { get; set; } = string.Empty;

    public string Persona { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public List<string> PeerGroupIds { get; set; } = new();

    /// <summary>Normalized handle without @.</summary>
    public string ResolvedUsername => FigureHandles.ResolveUsername(Username, Name);

    /// <summary>Author string for posts/replies: "@Handle".</summary>
    public string AtHandle => FigureHandles.AtHandle(Username, Name);
}
