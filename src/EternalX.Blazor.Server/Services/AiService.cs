namespace EternalX.Blazor.Server.Services;

public class AiService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;

    public AiService(IConfiguration config, HttpClient httpClient)
    {
        _config = config;
        _httpClient = httpClient;
    }

    public Task<string> GenerateReplyAsync(string prompt, string provider = "claude")
    {
        // TODO: Implement actual calls to Claude, OpenAI, Grok, Hugging Face using keys from _config.
        // Do not echo the full prompt: that amplified unbounded auto-reply growth in production.
        _ = prompt;
        var label = string.IsNullOrWhiteSpace(provider) ? "claude" : provider.Trim();
        return Task.FromResult($"[{label.ToUpperInvariant()}] Historical figure reply.");
    }
}