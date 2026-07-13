namespace EternalX.Blazor.Shared.Models;

public class FeedSettings
{
    public const string SingletonId = "default";

    public string Id { get; set; } = SingletonId;
    public bool AutoReplyPaused { get; set; }
    public string? DefaultAiProvider { get; set; }
    public Dictionary<string, string> ProviderModels { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> ProviderEfforts { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Provider names (claude, openai, grok, huggingface) that still have keys but
    /// must not be selected for generation. Keys stay in env; this is operator toggle only.
    /// </summary>
    public List<string> DisabledProviders { get; set; } = new();

    public bool IsProviderEnabled(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            return false;
        return !DisabledProviders.Any(d =>
            string.Equals(d, providerName, StringComparison.OrdinalIgnoreCase));
    }

    public void SetProviderEnabled(string providerName, bool enabled)
    {
        var name = providerName.Trim();
        DisabledProviders.RemoveAll(d =>
            string.Equals(d, name, StringComparison.OrdinalIgnoreCase));
        if (!enabled)
            DisabledProviders.Add(name);
    }
}
