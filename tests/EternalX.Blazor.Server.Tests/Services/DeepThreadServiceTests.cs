using EternalX.Blazor.Server.Data;
using EternalX.Blazor.Server.Services;
using EternalX.Blazor.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace EternalX.Blazor.Server.Tests.Services;

/// <summary>TEST-AI-002 bounded deep thread with mocked provider.</summary>
public class DeepThreadServiceTests : IDisposable
{
    private readonly string _path;
    private readonly LiteDbService _db;

    public DeepThreadServiceTests()
    {
        _path = Path.Combine(Path.GetTempPath(), "eternalx-tests", $"{Guid.NewGuid():N}.db");
        _db = new LiteDbService(_path);
    }

    public void Dispose()
    {
        try { if (File.Exists(_path)) File.Delete(_path); } catch { }
    }

    private sealed class CountingProvider : IAiProvider
    {
        public int Calls;
        public string Name => "mock";
        public bool IsConfigured => true;

        public Task<AiResult> GenerateAsync(AiRequest request, CancellationToken cancellationToken = default)
        {
            Calls++;
            // Include figure name; do not echo huge prompts
            return Task.FromResult(new AiResult(
                $"{request.FigureName} answers thoughtfully.",
                Name,
                "mock-1"));
        }
    }

    [Fact]
    public async Task GenerateThread_adds_between_min_and_max_replies()
    {
        var provider = new CountingProvider();
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var ai = new AiService(config, _db, new[] { provider });
        var mod = new ModeratorService(_db, NullLogger<ModeratorService>.Instance);
        var deep = new DeepThreadService(_db, ai, mod, new NullFeedNotifier(), NullLogger<DeepThreadService>.Instance, minReplies: 5, maxReplies: 7);

        var post = new Post { Content = "What is truth?", Author = "human", AuthorUserId = "u1" };
        _db.SavePost(post);

        var added = await deep.GenerateThreadAsync(post.Id);
        var loaded = _db.GetPost(post.Id)!;

        Assert.InRange(added, 5, 7);
        Assert.Equal(added, loaded.Replies.Count);
        Assert.All(loaded.Replies, r =>
        {
            Assert.True(r.IsAi);
            Assert.False(string.IsNullOrEmpty(r.FigureId));
            Assert.Equal("mock", r.Provider);
            Assert.DoesNotContain("What is truth?", r.Content); // not full prompt echo of post alone ok if figure answers - content is short
        });
        Assert.InRange(provider.Calls, 5, 7);
    }
}
