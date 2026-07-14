using System.Net.Http.Json;
using EternalX.Blazor.Shared.Models;

namespace EternalX.Blazor.Client.Services;

public sealed class FeedApiClient
{
    private readonly HttpClient _http;

    public FeedApiClient(HttpClient http) => _http = http;

    public Task<MeDto?> GetMeAsync() => _http.GetFromJsonAsync<MeDto>("api/me");

    public Task<AiStatusDto?> GetAiStatusAsync() => _http.GetFromJsonAsync<AiStatusDto>("api/ai/status");

    public async Task<List<Post>> GetPostsAsync()
        => await _http.GetFromJsonAsync<List<Post>>("api/posts") ?? new();

    public Task<Post?> GetPostAsync(Guid id)
        => _http.GetFromJsonAsync<Post>($"api/posts/{id}");

    public async Task<HashtagFeedDto?> GetHashtagFeedAsync(string tag)
    {
        var encoded = Uri.EscapeDataString(tag.TrimStart('#'));
        return await _http.GetFromJsonAsync<HashtagFeedDto>($"api/hashtags/{encoded}");
    }

    public Task<HttpResponseMessage> CreatePostAsync(string content, string? title = null)
        => _http.PostAsJsonAsync("api/posts", new { Content = content, Title = title });

    public Task<HttpResponseMessage> CreateReplyAsync(Guid postId, string content)
        => _http.PostAsJsonAsync($"api/posts/{postId}/replies", new { Content = content });

    public Task<HttpResponseMessage> LikePostAsync(Guid postId)
        => _http.PostAsync($"api/posts/{postId}/like", null);

    public Task<HttpResponseMessage> LikeReplyAsync(Guid postId, Guid replyId)
        => _http.PostAsync($"api/posts/{postId}/replies/{replyId}/like", null);

    /// <summary>Quote-reshare as a new post (optional comment).</summary>
    public Task<HttpResponseMessage> ResharePostAsync(Guid postId, string? comment = null)
        => _http.PostAsJsonAsync($"api/posts/{postId}/reshare", new { Comment = comment });

    public Task<AdminStatsDto?> GetAdminStatsAsync()
        => _http.GetFromJsonAsync<AdminStatsDto>("api/admin/stats");

    public Task<AdminAgentsDto?> GetAdminAgentsAsync()
        => _http.GetFromJsonAsync<AdminAgentsDto>("api/admin/agents");

    public Task<PersonalityEngagementReportDto?> GetPersonalityEngagementAsync()
        => _http.GetFromJsonAsync<PersonalityEngagementReportDto>("api/admin/personalities/engagement");

    public Task<HttpResponseMessage> EnableAgentAsync(string name)
        => _http.PostAsync($"api/admin/agents/{Uri.EscapeDataString(name)}/enable", null);

    public Task<HttpResponseMessage> DisableAgentAsync(string name)
        => _http.PostAsync($"api/admin/agents/{Uri.EscapeDataString(name)}/disable", null);

    public Task<HttpResponseMessage> SetDefaultAgentAsync(string name)
        => _http.PostAsJsonAsync("api/admin/agents/default", new { Name = name });

    public Task<HttpResponseMessage> PauseAutoReplyAsync()
        => _http.PostAsync("api/admin/auto-reply/pause", null);

    public Task<HttpResponseMessage> ResumeAutoReplyAsync()
        => _http.PostAsync("api/admin/auto-reply/resume", null);

    public Task<HttpResponseMessage> SeedAsync()
        => _http.PostAsync("api/admin/seed", null);

    public Task<HttpResponseMessage> ClearFeedAsync()
        => _http.PostAsync("api/admin/clear-feed", null);

    public Task<HttpResponseMessage> ExportAsync()
        => _http.GetAsync("api/admin/export");

    public Task<HttpResponseMessage> RestoreAsync(Stream jsonStream)
    {
        var content = new StreamContent(jsonStream);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        return _http.PostAsync("api/admin/restore", content);
    }
}

public sealed record HashtagFeedDto(
    string Tag,
    int Count,
    List<Post>? Posts);

public sealed record MeDto(
    bool Authenticated,
    string? UserId,
    string? Name,
    string? Email,
    bool IsAdmin,
    bool IsBanned,
    bool Gateway);

public sealed record AiStatusDto(
    bool Paused,
    List<string>? Providers,
    List<string>? LiveProviders,
    bool Live,
    bool UsingStub,
    string? DefaultProvider,
    int FigureCount,
    int PostCount,
    string? SignalR);

public sealed record AdminStatsDto(
    int PostCount,
    int ReplyCount,
    int FigureCount,
    int EnabledFigures,
    bool AutoReplyPaused);

public sealed record AdminAgentsDto(
    string? DefaultProvider,
    List<AgentDto>? Agents);

public sealed record AgentDto(
    string Name,
    bool HasApiKey,
    bool Enabled,
    bool Active);

public sealed record PersonalityEngagementReportDto(
    DateTime GeneratedAt,
    int PostSampleSize,
    List<PersonalityEngagementDto>? Personalities);

public sealed record PersonalityEngagementDto(
    string FigureId,
    string Name,
    string? Username,
    bool Enabled,
    List<string>? PeerGroupIds,
    int PostsAuthored,
    int RepliesAuthored,
    int LikesReceived,
    int ResharesReceived,
    int RepliesReceived,
    int MentionsReceived,
    int EngagementScore);
