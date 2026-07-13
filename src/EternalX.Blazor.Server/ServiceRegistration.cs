using EternalX.Blazor.Server.Data;
using EternalX.Blazor.Server.Services;

namespace EternalX.Blazor.Server;

/// <summary>
/// Shared DI registrations used by Program and composition-root tests.
/// Keeps the AiService activation path explicit and unambiguous.
/// </summary>
public static class ServiceRegistration
{
    /// <summary>
    /// Registers LiteDB, HTTP clients, AiService (explicit factory), moderator, and deep thread.
    /// Callers register <see cref="IFeedNotifier"/> and hosted services as needed.
    /// </summary>
    public static IServiceCollection AddEternalXApplicationServices(this IServiceCollection services)
    {
        services.AddHttpClient(AiService.AiHttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(45);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("EternalX.Blazor/1.0");
        });
        services.AddHttpClient();

        services.AddSingleton<LiteDbService>();

        // Explicit factory: only the public IHttpClientFactory constructor is used.
        // Do not use services.AddSingleton&lt;AiService&gt;() — multiple public ctors reintroduced
        // later would crash the host at startup (production incident 2026.7.21).
        services.AddSingleton(sp => new AiService(
            sp.GetRequiredService<IConfiguration>(),
            sp.GetRequiredService<IHttpClientFactory>(),
            sp.GetRequiredService<LiteDbService>(),
            sp.GetService<ILogger<AiService>>()));

        services.AddSingleton<ModeratorService>();
        services.AddSingleton(new AutoReplyOptions());
        services.AddSingleton<DeepThreadService>();

        return services;
    }
}
