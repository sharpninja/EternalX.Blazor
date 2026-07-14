using EternalX.Blazor.Server.Data;
using EternalX.Blazor.Shared.Models;

namespace EternalX.Blazor.Server.Services;

public class AiService
{
    public const string AiHttpClientName = "eternalx-ai";

    public static readonly string[] KnownProviderNames =
    [
        "claude",
        "openai",
        "grok",
        "huggingface"
    ];

    private readonly IReadOnlyList<IAiProvider> _providers;
    private readonly IAiProvider _stub;
    private readonly IConfiguration _config;
    private readonly LiteDbService _db;
    private readonly ILogger<AiService>? _logger;
    private int _roundRobin;
    private string? _lastError;

    /// <summary>
    /// Sole public constructor for DI. Do not add other public constructors whose
    /// parameters are all container-resolvable (causes ambiguous activation).
    /// </summary>
    public AiService(
        IConfiguration config,
        IHttpClientFactory httpClientFactory,
        LiteDbService db,
        ILogger<AiService>? logger = null)
        : this(
            config,
            db,
            CreateDefaultProviders(config, httpClientFactory.CreateClient(AiHttpClientName), logger),
            new StubAiProvider(),
            logger)
    {
    }

    /// <summary>Test-only path with injectable providers (not used by the DI container).</summary>
    internal AiService(
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

    /// <summary>Providers that have API keys configured (regardless of admin enable toggle).</summary>
    public IReadOnlyList<string> KeyedProviderNames()
        => _providers.Where(p => p.IsConfigured).Select(p => p.Name).ToList();

    /// <summary>Live providers: have keys AND not disabled in FeedSettings.</summary>
    public IReadOnlyList<string> LiveProviderNames()
    {
        var settings = _db.GetSettings();
        return _providers
            .Where(p => p.IsConfigured && settings.IsProviderEnabled(p.Name))
            .Select(p => p.Name)
            .ToList();
    }

    /// <summary>Names shown to clients: enabled live providers or stub fallback.</summary>
    public IReadOnlyList<string> ConfiguredProviderNames()
    {
        var live = LiveProviderNames();
        return live.Count > 0 ? live : new[] { _stub.Name };
    }

    public bool HasLiveProviders => LiveProviderNames().Count > 0;

    /// <summary>Last provider failure message (for admin diagnostics; no secrets).</summary>
    public string? LastError => _lastError;

    /// <summary>Admin inventory: key presence + enable flag without exposing secrets.</summary>
    public IReadOnlyList<AiAgentStatus> GetAgentStatuses()
    {
        var settings = _db.GetSettings();
        return _providers.Select(p => new AiAgentStatus(
            Name: p.Name,
            HasApiKey: p.IsConfigured,
            Enabled: settings.IsProviderEnabled(p.Name),
            Active: p.IsConfigured && settings.IsProviderEnabled(p.Name)
        )).ToList();
    }

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

        var provider = ResolveProvider(providerName, rotate: false, settings);
        settings.ProviderModels.TryGetValue(provider.Name, out var model);
        settings.ProviderEfforts.TryGetValue(provider.Name, out var effort);

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
            Effort: effort,
            Username: figure.ResolvedUsername);

        try
        {
            var result = await provider.GenerateAsync(request, cancellationToken).ConfigureAwait(false);
            _lastError = null;
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
            _lastError = $"{provider.Name}: {ex.Message}";
            _logger?.LogError(
                ex,
                "AI provider {Provider} failed for figure {Figure}; falling back to stub. Error={Error}",
                provider.Name,
                figure.Name,
                ex.Message);

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
        var settings = _db.GetSettings();
        var provider = ResolveProvider(null, rotate: true, settings);
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

    private IAiProvider ResolveProvider(string? preferredName, bool rotate, FeedSettings settings)
    {
        // Only providers with keys AND not admin-disabled.
        var usable = _providers
            .Where(p => p.IsConfigured && settings.IsProviderEnabled(p.Name))
            .ToList();

        if (usable.Count == 0)
            return _stub;

        if (!string.IsNullOrWhiteSpace(preferredName))
        {
            var match = usable.FirstOrDefault(p =>
                string.Equals(p.Name, preferredName, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
                return match;
            // Preferred is missing/disabled: fall through to default among usable.
        }

        if (rotate)
        {
            var idx = Interlocked.Increment(ref _roundRobin);
            return usable[Math.Abs(idx) % usable.Count];
        }

        var def = settings.DefaultAiProvider ?? _config["DEFAULT_AI_PROVIDER"];
        if (!string.IsNullOrWhiteSpace(def))
        {
            var preferred = usable.FirstOrDefault(p =>
                string.Equals(p.Name, def, StringComparison.OrdinalIgnoreCase));
            if (preferred is not null)
                return preferred;
        }

        return usable[0];
    }
}

public sealed record AiAgentStatus(string Name, bool HasApiKey, bool Enabled, bool Active);
