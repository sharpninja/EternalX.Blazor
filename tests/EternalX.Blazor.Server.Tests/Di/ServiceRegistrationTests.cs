using EternalX.Blazor.Server;
using EternalX.Blazor.Server.Data;
using EternalX.Blazor.Server.Services;
using EternalX.Blazor.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EternalX.Blazor.Server.Tests.Di;

/// <summary>
/// Composition-root regression: AiService must resolve from the same registrations
/// Program uses. Ambiguous constructors previously crash-looped production (2026.7.21).
/// </summary>
public class ServiceRegistrationTests : IDisposable
{
    private readonly string _dbPath;

    public ServiceRegistrationTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), "eternalx-tests", $"di-{Guid.NewGuid():N}.db");
    }

    public void Dispose()
    {
        try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch { }
    }

    [Fact]
    public async Task Real_di_container_resolves_AiService_without_ai_keys()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LITEDB_PATH"] = _dbPath,
                ["GATEWAY_KEY"] = "test-gateway-key-not-for-production",
                ["DEFAULT_AI_PROVIDER"] = "grok"
                // Intentionally no ANTHROPIC_API_KEY / XAI_API_KEY / etc.
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddLogging();
        services.AddEternalXApplicationServices();
        services.AddSingleton<IFeedNotifier, NullFeedNotifier>();

        await using var sp = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        // Must not throw InvalidOperationException (ambiguous constructors).
        var ai = sp.GetRequiredService<AiService>();
        Assert.NotNull(ai);
        Assert.False(ai.HasLiveProviders);
        Assert.Contains("stub", ai.ConfiguredProviderNames());

        // Dependent services that take AiService must also activate.
        var deep = sp.GetRequiredService<DeepThreadService>();
        Assert.NotNull(deep);
        var mod = sp.GetRequiredService<ModeratorService>();
        Assert.NotNull(mod);
        var db = sp.GetRequiredService<LiteDbService>();
        Assert.NotNull(db);

        // Stub generation works with zero keys.
        var result = await ai.GenerateForFigureAsync(
            new Figure { Id = "t", Name = "Test", Persona = "p" },
            "hello");
        Assert.Equal("stub", result.Provider);
        Assert.False(string.IsNullOrWhiteSpace(result.Text));
    }

    [Fact]
    public void AiService_has_exactly_one_public_constructor()
    {
        var publicCtors = typeof(AiService).GetConstructors(
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        Assert.Single(publicCtors);
        var parameters = publicCtors[0].GetParameters().Select(p => p.ParameterType).ToArray();
        Assert.Contains(typeof(IConfiguration), parameters);
        Assert.Contains(typeof(IHttpClientFactory), parameters);
        Assert.Contains(typeof(LiteDbService), parameters);
    }
}
