using EternalX.Blazor.Server.Data;
using EternalX.Blazor.Server.Services;
using EternalX.Blazor.Shared.Models;
using Microsoft.Extensions.Configuration;

namespace EternalX.Blazor.Server.Tests.Services;

/// <summary>Admin can disable agents without removing API keys.</summary>
public class AiAgentToggleTests : IDisposable
{
    private readonly string _path;
    private readonly LiteDbService _db;

    public AiAgentToggleTests()
    {
        _path = Path.Combine(Path.GetTempPath(), "eternalx-tests", $"{Guid.NewGuid():N}.db");
        _db = new LiteDbService(_path);
    }

    public void Dispose()
    {
        try { if (File.Exists(_path)) File.Delete(_path); } catch { }
    }

    private sealed class NamedProvider : IAiProvider
    {
        public NamedProvider(string name, bool configured)
        {
            Name = name;
            IsConfigured = configured;
        }

        public string Name { get; }
        public bool IsConfigured { get; }

        public Task<AiResult> GenerateAsync(AiRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new AiResult($"{Name}-ok", Name, "m1"));
    }

    [Fact]
    public void FeedSettings_disable_does_not_remove_keys_conceptually()
    {
        var s = new FeedSettings();
        Assert.True(s.IsProviderEnabled("grok"));
        s.SetProviderEnabled("grok", false);
        Assert.False(s.IsProviderEnabled("grok"));
        s.SetProviderEnabled("grok", true);
        Assert.True(s.IsProviderEnabled("grok"));
    }

    [Fact]
    public async Task Disabled_provider_with_key_is_not_selected()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DEFAULT_AI_PROVIDER"] = "grok"
            })
            .Build();

        var providers = new IAiProvider[]
        {
            new NamedProvider("grok", configured: true),
            new NamedProvider("claude", configured: true)
        };
        var ai = new AiService(config, _db, providers);

        var settings = _db.GetSettings();
        settings.SetProviderEnabled("grok", false);
        _db.SaveSettings(settings);

        var live = ai.LiveProviderNames();
        Assert.DoesNotContain("grok", live);
        Assert.Contains("claude", live);

        var statuses = ai.GetAgentStatuses();
        var grok = statuses.Single(a => a.Name == "grok");
        Assert.True(grok.HasApiKey);
        Assert.False(grok.Enabled);
        Assert.False(grok.Active);

        var figure = new Figure { Id = "f", Name = "Ada", Persona = "p" };
        var result = await ai.GenerateForFigureAsync(figure, "hi", preferredProvider: "grok");
        // Preferred grok disabled → falls to another usable provider (claude)
        Assert.Equal("claude", result.Provider);
    }

    [Fact]
    public async Task All_disabled_falls_back_to_stub()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var providers = new IAiProvider[]
        {
            new NamedProvider("grok", configured: true)
        };
        var ai = new AiService(config, _db, providers);

        var settings = _db.GetSettings();
        settings.SetProviderEnabled("grok", false);
        _db.SaveSettings(settings);

        Assert.Empty(ai.LiveProviderNames());
        Assert.Contains("stub", ai.ConfiguredProviderNames());

        var figure = new Figure { Id = "f", Name = "Ada", Persona = "p" };
        var result = await ai.GenerateForFigureAsync(figure, "hi");
        Assert.Equal("stub", result.Provider);
    }
}
