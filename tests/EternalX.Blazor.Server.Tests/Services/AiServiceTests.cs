using EternalX.Blazor.Server.Data;
using EternalX.Blazor.Server.Services;
using EternalX.Blazor.Shared.Models;
using Microsoft.Extensions.Configuration;

namespace EternalX.Blazor.Server.Tests.Services;

/// <summary>TEST-AI-004 server-side AI isolation / stub provider.</summary>
public class AiServiceTests : IDisposable
{
    private readonly string _path;
    private readonly LiteDbService _db;

    public AiServiceTests()
    {
        _path = Path.Combine(Path.GetTempPath(), "eternalx-tests", $"{Guid.NewGuid():N}.db");
        _db = new LiteDbService(_path);
    }

    public void Dispose()
    {
        try { if (File.Exists(_path)) File.Delete(_path); } catch { }
    }

    [Fact]
    public async Task GenerateReplyAsync_does_not_echo_full_prompt()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var ai = new AiService(config, new HttpClient(), _db);
        var huge = new string('z', 20_000);

        var reply = await ai.GenerateReplyAsync(huge, "claude");

        Assert.DoesNotContain(huge, reply);
        Assert.True(reply.Length < 500, $"reply length {reply.Length}");
        Assert.Contains("[CLAUDE]", reply);
    }

    [Fact]
    public async Task GenerateForFigure_uses_stub_when_no_keys_and_records_metadata()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var ai = new AiService(config, _db, Array.Empty<IAiProvider>());
        var figure = new Figure { Id = "f1", Name = "Socrates", Persona = "Ask questions." };

        var result = await ai.GenerateForFigureAsync(figure, "What is virtue?");

        Assert.Equal("stub", result.Provider);
        Assert.Equal("stub-1", result.Model);
        Assert.Contains("Socrates", result.Text);
        Assert.DoesNotContain(new string('x', 1000), result.Text);
    }

    [Fact]
    public void ConfiguredProviderNames_falls_back_to_stub()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var ai = new AiService(config, _db, Array.Empty<IAiProvider>());
        var names = ai.ConfiguredProviderNames();
        Assert.Contains("stub", names);
    }
}
