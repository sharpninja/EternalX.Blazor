namespace EternalX.Blazor.Shared.Models;

public class FeedSettings
{
    public const string SingletonId = "default";

    public string Id { get; set; } = SingletonId;
    public bool AutoReplyPaused { get; set; }
    public string? DefaultAiProvider { get; set; }
    public Dictionary<string, string> ProviderModels { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> ProviderEfforts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
