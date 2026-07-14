using EternalX.Blazor.Server.Data;
using EternalX.Blazor.Shared.Models;

namespace EternalX.Blazor.Server.Tests.Data;

/// <summary>TEST-CORE-001 / TEST-CORE-004: LiteDB round-trip, seed, concurrency.</summary>
public class LiteDbServiceTests : IDisposable
{
    private readonly string _path;
    private readonly LiteDbService _db;

    public LiteDbServiceTests()
    {
        _path = Path.Combine(Path.GetTempPath(), "eternalx-tests", $"{Guid.NewGuid():N}.db");
        _db = new LiteDbService(_path);
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_path))
                File.Delete(_path);
        }
        catch
        {
            // best-effort cleanup
        }
    }

    [Fact]
    public void Seed_inserts_default_figures_and_peer_groups()
    {
        var figures = _db.GetFigures();
        var groups = _db.GetPeerGroups();

        Assert.Equal(46, figures.Count);
        Assert.Equal(8, groups.Count);
        Assert.Contains(figures, f => f.Id == "fig-socrates");
        Assert.Contains(groups, g => g.Id == "philosophers");
        Assert.Contains(groups, g => g.Id == "stage-screen");
        var socrates = figures.Single(f => f.Id == "fig-socrates");
        Assert.Equal("GadflyAthens", socrates.Username);
        Assert.Equal("@GadflyAthens", socrates.AtHandle);
        Assert.Contains(figures, f => f.Username == "StarmanZiggy");
    }

    [Fact]
    public void Seed_twice_does_not_clobber_operator_edits()
    {
        var edited = _db.GetFigure("fig-socrates")!;
        edited.Persona = "Operator-custom persona";
        edited.Username = "MyCustomHandle";
        _db.UpsertFigure(edited);

        _db.EnsureSeeded();

        var again = _db.GetFigure("fig-socrates")!;
        Assert.Equal("Operator-custom persona", again.Persona);
        Assert.Equal("MyCustomHandle", again.Username);
    }

    [Fact]
    public void Seed_backfills_empty_username_without_clobbering_persona()
    {
        var edited = _db.GetFigure("fig-shakespeare")!;
        edited.Username = "";
        edited.Persona = "Keep this persona";
        _db.UpsertFigure(edited);

        _db.EnsureSeeded();

        var again = _db.GetFigure("fig-shakespeare")!;
        Assert.Equal("BardOfAvon", again.Username);
        Assert.Equal("Keep this persona", again.Persona);
    }

    [Fact]
    public void Seed_matches_shared_network_roster_size()
    {
        Assert.Equal(46, DefaultRoster.Figures().Count);
        Assert.Equal(8, DefaultRoster.PeerGroups().Count);
        // Names align with EternalReddit DefaultRoster (no Christopher Columbus).
        Assert.DoesNotContain(DefaultRoster.Figures(), f => f.Name.Contains("Columbus", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Save_and_get_post_round_trip()
    {
        var post = new Post
        {
            Content = "What is virtue?",
            Author = "mortal",
            AuthorUserId = "u1",
            Title = "Inquiry"
        };
        _db.SavePost(post);

        var loaded = _db.GetPost(post.Id);
        Assert.NotNull(loaded);
        Assert.Equal("What is virtue?", loaded!.Content);
        Assert.Equal("Inquiry", loaded.Title);
        Assert.Equal("u1", loaded.AuthorUserId);
    }

    [Fact]
    public async Task CommitReplyAsync_concurrent_appends_do_not_drop_replies()
    {
        var post = new Post { Content = "root", Author = "human", AuthorUserId = "u1" };
        _db.SavePost(post);

        var tasks = Enumerable.Range(0, 20).Select(i =>
            _db.CommitReplyAsync(post.Id, new Reply
            {
                Content = $"reply-{i}",
                Author = $"fig-{i}",
                IsAi = true,
                CreatedAt = DateTime.UtcNow
            }));

        await Task.WhenAll(tasks);

        var loaded = _db.GetPost(post.Id);
        Assert.NotNull(loaded);
        Assert.Equal(20, loaded!.Replies.Count);
    }

    [Fact]
    public void ClearFeed_preserves_figures_and_settings()
    {
        _db.SavePost(new Post { Content = "gone", Author = "a" });
        var settings = _db.GetSettings();
        settings.AutoReplyPaused = true;
        _db.SaveSettings(settings);

        _db.ClearFeed();

        Assert.Equal(0, _db.CountPosts());
        Assert.True(_db.GetFigures().Count >= 10);
        Assert.True(_db.GetSettings().AutoReplyPaused);
    }

    [Fact]
    public void Export_and_restore_round_trip_rejects_bad_version()
    {
        _db.SavePost(new Post { Content = "keep", Author = "a" });
        var bundle = _db.CreateExport();
        Assert.Equal(ExportBundle.CurrentVersion, bundle.Version);
        Assert.NotEmpty(bundle.Posts);

        Assert.Throws<InvalidOperationException>(() =>
            _db.RestoreExport(new ExportBundle { Version = 99 }));

        _db.ClearFeed();
        Assert.Equal(0, _db.CountPosts());

        _db.RestoreExport(bundle);
        Assert.Equal(1, _db.CountPosts());
    }
}
