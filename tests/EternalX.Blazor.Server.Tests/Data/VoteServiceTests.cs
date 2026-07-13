using EternalX.Blazor.Server.Data;
using EternalX.Blazor.Shared.Models;

namespace EternalX.Blazor.Server.Tests.Data;

/// <summary>X-style like (heart) toggle via LiteDB.</summary>
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
    public async Task Like_toggle_once_per_user()
    {
        var post = new Post { Content = "hello #history @Ada", Author = "a" };
        _db.SavePost(post);

        var (p1, liked1) = await _db.TogglePostLikeAsync("u1", post.Id);
        Assert.True(liked1);
        Assert.Equal(1, p1!.LikeCount);

        var (p2, liked2) = await _db.TogglePostLikeAsync("u1", post.Id);
        Assert.False(liked2);
        Assert.Equal(0, p2!.LikeCount);

        var (p3, liked3) = await _db.TogglePostLikeAsync("u1", post.Id);
        Assert.True(liked3);
        Assert.Equal(1, p3!.LikeCount);
    }

    [Fact]
    public async Task Quote_reshare_creates_new_post_not_reply()
    {
        var source = new Post { Content = "Original #wisdom", Author = "Socrates", IsAi = true };
        _db.SavePost(source);

        var quote = new Post
        {
            Content = "Agree with @Socrates #philosophy",
            Author = "mortal",
            AuthorUserId = "u1",
            CreatedAt = DateTime.UtcNow
        };

        var created = await _db.CreateQuoteReshareAsync(source.Id, quote);
        Assert.NotNull(created);
        Assert.Equal(source.Id, created!.QuotedPostId);
        Assert.Equal("Socrates", created.QuotedAuthor);
        Assert.Equal("Original #wisdom", created.QuotedContent);
        Assert.Contains("Socrates", created.Mentions);
        Assert.Contains("philosophy", created.Hashtags);
        Assert.Empty(created.Replies);

        var src = _db.GetPost(source.Id)!;
        Assert.Equal(1, src.ReshareCount);

        // Quote is its own top-level post
        var recent = _db.GetRecentPosts().ToList();
        Assert.Contains(recent, p => p.Id == created.Id);
        Assert.DoesNotContain(src.Replies, r => r.Content.Contains("Agree"));
    }
}
