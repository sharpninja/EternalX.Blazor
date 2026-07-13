using EternalX.Blazor.Server.Data;
using EternalX.Blazor.Shared.Models;

namespace EternalX.Blazor.Server.Services;

public class AiService
{
    public const string AiHttpClientName = "eternalx-ai";

    private readonly IReadOnlyList<IAiProvider> _providers;
    private readonly IAiProvider _stub;
    private readonly IConfiguration _config;
    private readonly LiteDbService _db;
    private readonly ILogger<AiService>? _logger;
    private int _roundRobin;

    public AiService(
        IConfiguration config,
        IHttpClientFactory httpClientFactory,
        LiteDbService db,
        ILogger<AiService>? logger = null)
        : this(config, db, CreateDefaultProviders(config, httpClientFactory.CreateClient(AiHttpClientName), logger), new StubAiProvider(), logger)
    {
    }

    /// <summary>Legacy/test constructor with a single HttpClient.</summary>
    public AiService(IConfiguration config, HttpClient httpClient, LiteDbService db)
        : this(config, db, CreateDefaultProviders(config, httpClient, null), new StubAiProvider(), null)
    {
    }

    /// <summary>Test constructor with injectable providers.</summary>
    public AiService(
        IConfiguration config,
        LiteDbService db,
        IEnumerable<IAiProvider> providers,
        IAiProvider? stub = null,
        ILogger<AiService>? logger = null)
    {
        _config = config;
        _db = db;
        _stub = stub ?? new StubAiProvider();
        _providers = providers.ToList();
        _logger = logger;
    }

    private static IReadOnlyList<IAiProvider> CreateDefaultProviders(
        IConfiguration config,
        HttpClient http,
        ILogger? logger) =>
    [
        new ClaudeAiProvider(config, http, logger),
        new OpenAiProvider(config, http, logger),
        new GrokAiProvider(config, http, logger),
        new HuggingFaceAiProvider(config, http, logger)
    ];

    /// <summary>Live (configured) provider names only; empty when all missing keys.</summary>
    public IReadOnlyList<string> LiveProviderNames()
        => _providers.Where(p => p.IsConfigured).Select(p => p.Name).ToList();

    /// <summary>Names shown to clients: live providers or stub fallback.</summary>
    public IReadOnlyList<string> ConfiguredProviderNames()
    {
        var live = LiveProviderNames();
        return live.Count > 0 ? live : new[] { _stub.Name };
    }

    public bool HasLiveProviders => LiveProviderNames().Count > 0;

    public async Task<AiResult> GenerateForFigureAsync(
        Figure figure,
        string userPrompt,
        string? preferredProvider = null,
        CancellationToken cancellationToken = default)
    {
        var settings = _db.GetSettings();
        var providerName = preferredProvider
            ?? settings.DefaultAiProvider
            ?? _config["DEFAULT_AI_PROVIDER"]
            ?? "grok";

        var provider = ResolveProvider(providerName, rotate: false);
        settings.ProviderModels.TryGetValue(provider.Name, out var model);
        settings.ProviderEfforts.TryGetValue(provider.Name, out var effort);

        // Env model overrides when settings do not set one.
        model ??= provider.Name switch
        {
            "claude" => FirstConfig("ANTHROPIC_MODEL", "CLAUDE_MODEL"),
            "openai" => FirstConfig("OPENAI_MODEL"),
            "grok" => FirstConfig("XAI_MODEL", "GROK_MODEL"),
            "huggingface" => FirstConfig("HUGGINGFACE_MODEL", "HF_MODEL"),
            _ => null
        };

        var request = new AiRequest(
            Persona: figure.Persona,
            FigureName: figure.Name,
            UserPrompt: userPrompt,
            Model: model,
            Effort: effort);

        try
        {
            var result = await provider.GenerateAsync(request, cancellationToken).ConfigureAwait(false);
            _logger?.LogInformation(
                "AI generate ok provider={Provider} model={Model} figure={Figure} chars={Chars}",
                result.Provider,
                result.Model,
                figure.Name,
                result.Text.Length);
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(
                ex,
                "AI provider {Provider} failed for figure {Figure}; falling back to stub",
                provider.Name,
                figure.Name);

            // Soft-fail to stub so feed never hard-crashes on provider outages.
            return await _stub.GenerateAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Legacy entry used by older call sites; assigns a generic persona.</summary>
    public async Task<string> GenerateReplyAsync(string prompt, string provider = "claude")
    {
        var figure = new Figure
        {
            Id = "legacy",
            Name = "Historical Figure",
            Persona = "A thoughtful historical figure speaking briefly and in character."
        };
        var result = await GenerateForFigureAsync(figure, prompt, provider).ConfigureAwait(false);
        if (result.Provider == "stub")
            return $"[{provider.ToUpperInvariant()}] {result.Text}";
        return result.Text;
    }

    public async Task<AiResult> GenerateRoundRobinAsync(
        Figure figure,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        var provider = ResolveProvider(null, rotate: true);
        return await GenerateForFigureAsync(figure, userPrompt, provider.Name, cancellationToken)
            .ConfigureAwait(false);
    }

    private string? FirstConfig(params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = _config[key];
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return null;
    }

    private IAiProvider ResolveProvider(string? preferredName, bool rotate)
    {
        var configured = _providers.Where(p => p.IsConfigured).ToList();
        if (configured.Count == 0)
            return _stub;

        if (!string.IsNullOrWhiteSpace(preferredName))
        {
            var match = configured.FirstOrDefault(p =>
                string.Equals(p.Name, preferredName, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
                return match;
        }

        if (rotate)
        {
            var idx = Interlocked.Increment(ref _roundRobin);
            return configured[Math.Abs(idx) % configured.Count];
        }

        // Prefer DEFAULT_AI_PROVIDER among live providers, else first live.
        var def = _config["DEFAULT_AI_PROVIDER"];
        if (!string.IsNullOrWhiteSpace(def))
        {
            var preferred = configured.FirstOrDefault(p =>
                string.Equals(p.Name, def, StringComparison.OrdinalIgnoreCase));
            if (preferred is not null)
                return preferred;
        }

        return configured[0];
    }
}
