using System.Net;
using System.Text;
using EternalX.Blazor.Server.Data;
using EternalX.Blazor.Server.Services;
using EternalX.Blazor.Shared.Models;
using Microsoft.Extensions.Configuration;

namespace EternalX.Blazor.Server.Tests.Services;

/// <summary>Live provider config detection + HTTP response parsing (mocked network).</summary>
public class LiveAiProviderTests : IDisposable
{
    private readonly string _path;
    private readonly LiteDbService _db;

    public LiveAiProviderTests()
    {
        _path = Path.Combine(Path.GetTempPath(), "eternalx-tests", $"{Guid.NewGuid():N}.db");
        _db = new LiteDbService(_path);
    }

    public void Dispose()
    {
        try { if (File.Exists(_path)) File.Delete(_path); } catch { }
    }

    [Fact]
    public void ParseClaude_extracts_text()
    {
        var json = """{"content":[{"type":"text","text":" Hello from Claude "}]}""";
        Assert.Equal("Hello from Claude", AiResponseParsers.ParseClaude(json));
    }

    [Fact]
    public void ParseOpenAiCompatible_extracts_text()
    {
        var json = """{"choices":[{"message":{"content":"Grok says hi"}}]}""";
        Assert.Equal("Grok says hi", AiResponseParsers.ParseOpenAiCompatible(json));
    }

    [Fact]
    public void ParseHuggingFace_extracts_generated_text()
    {
        var json = """[{"generated_text":"HF reply"}]""";
        Assert.Equal("HF reply", AiResponseParsers.ParseHuggingFace(json));
    }

    [Theory]
    [InlineData("ANTHROPIC_API_KEY", "claude")]
    [InlineData("CLAUDE_API_KEY", "claude")]
    [InlineData("OPENAI_API_KEY", "openai")]
    [InlineData("XAI_API_KEY", "grok")]
    [InlineData("GROK_API_KEY", "grok")]
    [InlineData("HUGGINGFACE_API_KEY", "huggingface")]
    public void Provider_IsConfigured_for_env_key_aliases(string key, string providerName)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [key] = "secret-test-key" })
            .Build();
        var http = new HttpClient(new FixedJsonHandler("""{"content":[{"type":"text","text":"x"}],"choices":[{"message":{"content":"x"}}],"generated_text":"x"}"""));

        IAiProvider provider = providerName switch
        {
            "claude" => new ClaudeAiProvider(config, http),
            "openai" => new OpenAiProvider(config, http),
            "grok" => new GrokAiProvider(config, http),
            "huggingface" => new HuggingFaceAiProvider(config, http),
            _ => throw new ArgumentOutOfRangeException(nameof(providerName))
        };

        Assert.True(provider.IsConfigured);
    }

    [Fact]
    public async Task ClaudeAiProvider_live_call_parses_response()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CLAUDE_API_KEY"] = "test-key",
                ["CLAUDE_MODEL"] = "claude-test"
            })
            .Build();

        var handler = new FixedJsonHandler("""{"content":[{"type":"text","text":"In character reply."}]}""");
        var provider = new ClaudeAiProvider(config, new HttpClient(handler));
        var result = await provider.GenerateAsync(new AiRequest("persona", "Socrates", "What is virtue?"));

        Assert.Equal("claude", result.Provider);
        Assert.Equal("claude-test", result.Model);
        Assert.Equal("In character reply.", result.Text);
        Assert.Contains("api.anthropic.com", handler.LastRequestUri?.Host ?? "");
    }

    [Fact]
    public async Task AiService_uses_live_provider_when_key_present()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OPENAI_API_KEY"] = "sk-test",
                ["DEFAULT_AI_PROVIDER"] = "openai"
            })
            .Build();

        var handler = new FixedJsonHandler("""{"choices":[{"message":{"content":"Live OpenAI line."}}]}""");
        var openai = new OpenAiProvider(config, new HttpClient(handler));
        var ai = new AiService(config, _db, new IAiProvider[] { openai });

        Assert.True(ai.HasLiveProviders);
        Assert.Contains("openai", ai.LiveProviderNames());
        Assert.DoesNotContain("stub", ai.ConfiguredProviderNames());

        var figure = new Figure { Id = "f", Name = "Ada", Persona = "Inventor" };
        var result = await ai.GenerateForFigureAsync(figure, "Hello");

        Assert.Equal("openai", result.Provider);
        Assert.Equal("Live OpenAI line.", result.Text);
    }

    [Fact]
    public async Task AiService_falls_back_to_stub_on_http_failure()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["OPENAI_API_KEY"] = "sk-test" })
            .Build();

        var failing = new FailingHandler();
        var openai = new OpenAiProvider(config, new HttpClient(failing));
        var ai = new AiService(config, _db, new IAiProvider[] { openai });

        var figure = new Figure { Id = "f", Name = "Ada", Persona = "Inventor" };
        var result = await ai.GenerateForFigureAsync(figure, "Hello");

        Assert.Equal("stub", result.Provider);
        Assert.Contains("Ada", result.Text);
    }

    private sealed class FixedJsonHandler : HttpMessageHandler
    {
        private readonly string _json;
        public Uri? LastRequestUri { get; private set; }

        public FixedJsonHandler(string json) => _json = json;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_json, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class FailingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("boom")
            });
    }
}
