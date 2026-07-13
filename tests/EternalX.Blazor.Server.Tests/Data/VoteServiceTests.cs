using EternalX.Blazor.Server.Data;
using EternalX.Blazor.Shared.Models;

namespace EternalX.Blazor.Server.Tests.Data;

/// <summary>TEST-CORE-001 vote dedupe counters via LiteDB.</summary>
public class VoteServiceTests : IDisposable
{
    private readonly string _path;
    private readonly LiteDbService _db;

    public VoteServiceTests()
    {
        _path = Path.Combine(Path.GetTempPath(), "eternalx-tests", $"{Guid.NewGuid():N}.db");
        _db = new LiteDbService(_path);
    }

    public void Dispose()
    {
        try { if (File.Exists(_path)) File.Delete(_path); } catch { }
    }

    [Fact]
    public async Task Vote_toggle_and_switch_updates_counters_once_per_user()
    {
        var post = new Post { Content = "hello", Author = "a" };
        _db.SavePost(post);

        await _db.ApplyPostVoteAsync("u1", post.Id, 1);
        var p1 = _db.GetPost(post.Id)!;
        Assert.Equal(1, p1.Upvotes);
        Assert.Equal(0, p1.Downvotes);

        await _db.ApplyPostVoteAsync("u1", post.Id, 1);
        var p2 = _db.GetPost(post.Id)!;
        Assert.Equal(1, p2.Upvotes);

        await _db.ApplyPostVoteAsync("u1", post.Id, -1);
        var p3 = _db.GetPost(post.Id)!;
        Assert.Equal(0, p3.Upvotes);
        Assert.Equal(1, p3.Downvotes);

        await _db.ApplyPostVoteAsync("u1", post.Id, 0);
        var p4 = _db.GetPost(post.Id)!;
        Assert.Equal(0, p4.Upvotes);
        Assert.Equal(0, p4.Downvotes);
    }
}
