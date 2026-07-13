using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace EternalX.Blazor.Server.Services;

/// <summary>
/// Live HTTP AI providers. Configured when API keys are present (supports both
/// product env names and vendor-native names from .env.example).
/// </summary>
public abstract class EnvHttpAiProvider : IAiProvider
{
    protected readonly IConfiguration Config;
    protected readonly HttpClient Http;
    protected readonly ILogger? Logger;

    protected EnvHttpAiProvider(IConfiguration config, HttpClient http, ILogger? logger = null)
    {
        Config = config;
        Http = http;
        Logger = logger;
    }

    public abstract string Name { get; }
    public abstract bool IsConfigured { get; }
    public abstract Task<AiResult> GenerateAsync(AiRequest request, CancellationToken cancellationToken = default);

    protected static string BuildSystem(AiRequest request) =>
        $"You are {request.FigureName}. Stay strictly in character. Persona: {request.Persona}. " +
        "Keep replies short (1-3 sentences). Be playful and affectionate. Do not break character. " +
        "Do not claim to be an AI model.";

    protected string? FirstConfig(params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = Config[key];
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return null;
    }
}

public sealed class ClaudeAiProvider : EnvHttpAiProvider
{
    public ClaudeAiProvider(IConfiguration config, HttpClient http, ILogger? logger = null)
        : base(config, http, logger) { }

    public override string Name => "claude";
    public override bool IsConfigured => FirstConfig("ANTHROPIC_API_KEY", "CLAUDE_API_KEY") is not null;

    public override async Task<AiResult> GenerateAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        var key = FirstConfig("ANTHROPIC_API_KEY", "CLAUDE_API_KEY")
                  ?? throw new InvalidOperationException("Claude API key missing.");
        var model = request.Model
                    ?? FirstConfig("ANTHROPIC_MODEL", "CLAUDE_MODEL")
                    ?? "claude-3-5-haiku-latest";

        using var msg = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        msg.Headers.TryAddWithoutValidation("x-api-key", key);
        msg.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");
        var body = new
        {
            model,
            max_tokens = 256,
            system = BuildSystem(request),
            messages = new[] { new { role = "user", content = request.UserPrompt } }
        };
        msg.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var res = await Http.SendAsync(msg, cancellationToken).ConfigureAwait(false);
        var raw = await res.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!res.IsSuccessStatusCode)
        {
            Logger?.LogWarning("Claude HTTP {Status}: {Body}", (int)res.StatusCode, Truncate(raw));
            res.EnsureSuccessStatusCode();
        }

        var text = AiResponseParsers.ParseClaude(raw);
        return new AiResult(text, Name, model);
    }

    private static string Truncate(string s) => s.Length <= 200 ? s : s[..200];
}

public sealed class OpenAiProvider : EnvHttpAiProvider
{
    public OpenAiProvider(IConfiguration config, HttpClient http, ILogger? logger = null)
        : base(config, http, logger) { }

    public override string Name => "openai";
    public override bool IsConfigured => FirstConfig("OPENAI_API_KEY") is not null;

    public override async Task<AiResult> GenerateAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        var key = FirstConfig("OPENAI_API_KEY")
                  ?? throw new InvalidOperationException("OpenAI API key missing.");
        var model = request.Model ?? FirstConfig("OPENAI_MODEL") ?? "gpt-4o-mini";
        var baseUrl = FirstConfig("OPENAI_BASE_URL") ?? "https://api.openai.com/v1";

        using var msg = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/chat/completions");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        var body = new
        {
            model,
            messages = new object[]
            {
                new { role = "system", content = BuildSystem(request) },
                new { role = "user", content = request.UserPrompt }
            },
            max_tokens = 256
        };
        msg.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var res = await Http.SendAsync(msg, cancellationToken).ConfigureAwait(false);
        var raw = await res.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!res.IsSuccessStatusCode)
        {
            Logger?.LogWarning("OpenAI HTTP {Status}: {Body}", (int)res.StatusCode, raw.Length <= 200 ? raw : raw[..200]);
            res.EnsureSuccessStatusCode();
        }

        return new AiResult(AiResponseParsers.ParseOpenAiCompatible(raw), Name, model);
    }
}

public sealed class GrokAiProvider : EnvHttpAiProvider
{
    public GrokAiProvider(IConfiguration config, HttpClient http, ILogger? logger = null)
        : base(config, http, logger) { }

    public override string Name => "grok";
    public override bool IsConfigured => FirstConfig("XAI_API_KEY", "GROK_API_KEY") is not null;

    public override async Task<AiResult> GenerateAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        var key = FirstConfig("XAI_API_KEY", "GROK_API_KEY")
                  ?? throw new InvalidOperationException("Grok/xAI API key missing.");
        var model = request.Model ?? FirstConfig("XAI_MODEL", "GROK_MODEL") ?? "grok-2-latest";
        var baseUrl = FirstConfig("XAI_BASE_URL", "GROK_BASE_URL") ?? "https://api.x.ai/v1";

        using var msg = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/chat/completions");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        var body = new
        {
            model,
            messages = new object[]
            {
                new { role = "system", content = BuildSystem(request) },
                new { role = "user", content = request.UserPrompt }
            },
            max_tokens = 256
        };
        msg.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var res = await Http.SendAsync(msg, cancellationToken).ConfigureAwait(false);
        var raw = await res.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!res.IsSuccessStatusCode)
        {
            Logger?.LogWarning("Grok HTTP {Status}: {Body}", (int)res.StatusCode, raw.Length <= 200 ? raw : raw[..200]);
            res.EnsureSuccessStatusCode();
        }

        return new AiResult(AiResponseParsers.ParseOpenAiCompatible(raw), Name, model);
    }
}

public sealed class HuggingFaceAiProvider : EnvHttpAiProvider
{
    public HuggingFaceAiProvider(IConfiguration config, HttpClient http, ILogger? logger = null)
        : base(config, http, logger) { }

    public override string Name => "huggingface";
    public override bool IsConfigured => FirstConfig("HUGGINGFACE_API_KEY", "HF_API_KEY") is not null;

    public override async Task<AiResult> GenerateAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        var key = FirstConfig("HUGGINGFACE_API_KEY", "HF_API_KEY")
                  ?? throw new InvalidOperationException("HuggingFace API key missing.");
        var model = request.Model
                    ?? FirstConfig("HUGGINGFACE_MODEL", "HF_MODEL")
                    ?? "HuggingFaceH4/zephyr-7b-beta";
        var baseUrl = FirstConfig("HUGGINGFACE_BASE_URL") ?? "https://api-inference.huggingface.co/models";

        using var msg = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/{model}");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        var prompt = $"{BuildSystem(request)}\n\nUser: {request.UserPrompt}\nAssistant:";
        msg.Content = new StringContent(JsonSerializer.Serialize(new { inputs = prompt, parameters = new { max_new_tokens = 128, return_full_text = false } }), Encoding.UTF8, "application/json");

        using var res = await Http.SendAsync(msg, cancellationToken).ConfigureAwait(false);
        var raw = await res.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!res.IsSuccessStatusCode)
        {
            Logger?.LogWarning("HuggingFace HTTP {Status}: {Body}", (int)res.StatusCode, raw.Length <= 200 ? raw : raw[..200]);
            res.EnsureSuccessStatusCode();
        }

        return new AiResult(AiResponseParsers.ParseHuggingFace(raw), Name, model);
    }
}
