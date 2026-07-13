using LiteDB;
using EternalX.Blazor.Shared.Models;

namespace EternalX.Blazor.Server.Data;

public class LiteDbService
{
    /// <summary>
    /// LiteDB's global BsonMapper is not safe under concurrent serialize from
    /// parallel test/host threads; gate all open/serialize operations.
    /// </summary>
    private static readonly object DbGate = new();

    private readonly string _dbPath;
    private readonly SemaphoreSlim _postLock = new(1, 1);

    public LiteDbService(IConfiguration config)
    {
        _dbPath = config["LITEDB_PATH"] ?? "/app/data/eternalx.db";
        var dir = Path.GetDirectoryName(_dbPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        EnsureSeeded();
    }

    public LiteDbService(string dbPath)
    {
        _dbPath = dbPath;
        var dir = Path.GetDirectoryName(_dbPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        EnsureSeeded();
    }

    public string DatabasePath => _dbPath;

    public LiteDatabase GetDatabase()
    {
        lock (DbGate)
        {
            return new LiteDatabase(_dbPath);
        }
    }

    private T WithDb<T>(Func<LiteDatabase, T> action)
    {
        lock (DbGate)
        {
            using var db = new LiteDatabase(_dbPath);
            return action(db);
        }
    }

    private void WithDb(Action<LiteDatabase> action)
    {
        lock (DbGate)
        {
            using var db = new LiteDatabase(_dbPath);
            action(db);
        }
    }

    public void EnsureSeeded() => WithDb(SeedInto);

    internal static void SeedInto(LiteDatabase db)
    {
        var groups = db.GetCollection<PeerGroup>("peerGroups");
        foreach (var g in DefaultRoster.PeerGroups())
        {
            if (groups.FindById(g.Id) is null)
                groups.Insert(g);
        }

        var figures = db.GetCollection<Figure>("figures");
        foreach (var f in DefaultRoster.Figures())
        {
            if (figures.FindById(f.Id) is null)
                figures.Insert(f);
        }

        var settings = db.GetCollection<FeedSettings>("settings");
        if (settings.FindById(FeedSettings.SingletonId) is null)
            settings.Insert(new FeedSettings());
    }

    public IEnumerable<Post> GetRecentPosts(int count = 50) => WithDb(db =>
        db.GetCollection<Post>("posts")
            .FindAll()
            .OrderByDescending(p => p.CreatedAt)
            .Take(count)
            .ToList());

    public Post? GetPost(Guid id) => WithDb(db =>
        db.GetCollection<Post>("posts").FindById(id));

    public void SavePost(Post post) => WithDb(db =>
    {
        var col = db.GetCollection<Post>("posts");
        col.EnsureIndex(x => x.CreatedAt);
        col.Insert(post);
    });

    public void UpdatePost(Post post) => WithDb(db =>
        db.GetCollection<Post>("posts").Update(post));

    public async Task<Post?> CommitReplyAsync(Guid postId, Reply reply, CancellationToken cancellationToken = default)
    {
        await _postLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return WithDb(db =>
            {
                var col = db.GetCollection<Post>("posts");
                var post = col.FindById(postId);
                if (post is null)
                    return null;

                post.Replies ??= new List<Reply>();
                post.Replies.Add(reply);
                col.Update(post);
                return post;
            });
        }
        finally
        {
            _postLock.Release();
        }
    }

    public async Task<Post?> UpdatePostLockedAsync(Guid postId, Action<Post> mutate, CancellationToken cancellationToken = default)
    {
        await _postLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return WithDb(db =>
            {
                var col = db.GetCollection<Post>("posts");
                var post = col.FindById(postId);
                if (post is null)
                    return null;

                mutate(post);
                col.Update(post);
                return post;
            });
        }
        finally
        {
            _postLock.Release();
        }
    }

    /// <summary>Apply a vote without nested DB opens (FR-CORE-007).</summary>
    public async Task<Post?> ApplyPostVoteAsync(string userId, Guid postId, int value, CancellationToken cancellationToken = default)
    {
        if (value is not (-1 or 0 or 1))
            throw new ArgumentOutOfRangeException(nameof(value));

        await _postLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return WithDb(db =>
            {
                var posts = db.GetCollection<Post>("posts");
                var votes = db.GetCollection<Vote>("votes");
                var post = posts.FindById(postId);
                if (post is null)
                    return null;

                var voteId = Vote.MakeId(userId, VoteTargetType.Post, postId);
                var existing = votes.FindById(voteId);
                var prev = existing?.Value ?? 0;

                if (prev == 1) post.Upvotes = Math.Max(0, post.Upvotes - 1);
                if (prev == -1) post.Downvotes = Math.Max(0, post.Downvotes - 1);

                if (value == 0)
                {
                    votes.Delete(voteId);
                }
                else
                {
                    if (value == 1) post.Upvotes++;
                    if (value == -1) post.Downvotes++;
                    var vote = new Vote
                    {
                        Id = voteId,
                        UserId = userId,
                        TargetType = VoteTargetType.Post,
                        TargetId = postId,
                        Value = value,
                        UpdatedAt = DateTime.UtcNow
                    };
                    if (existing is null) votes.Insert(vote);
                    else votes.Update(vote);
                }

                posts.Update(post);
                return post;
            });
        }
        finally
        {
            _postLock.Release();
        }
    }

    public async Task<Post?> ApplyReplyVoteAsync(
        string userId,
        Guid postId,
        Guid replyId,
        int value,
        CancellationToken cancellationToken = default)
    {
        if (value is not (-1 or 0 or 1))
            throw new ArgumentOutOfRangeException(nameof(value));

        await _postLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return WithDb(db =>
            {
                var posts = db.GetCollection<Post>("posts");
                var votes = db.GetCollection<Vote>("votes");
                var post = posts.FindById(postId);
                if (post is null)
                    return null;

                var reply = post.Replies?.FirstOrDefault(r => r.Id == replyId);
                if (reply is null)
                    return null;

                var voteId = Vote.MakeId(userId, VoteTargetType.Reply, replyId);
                var existing = votes.FindById(voteId);
                var prev = existing?.Value ?? 0;

                if (prev == 1) reply.Upvotes = Math.Max(0, reply.Upvotes - 1);
                if (prev == -1) reply.Downvotes = Math.Max(0, reply.Downvotes - 1);

                if (value == 0)
                {
                    votes.Delete(voteId);
                }
                else
                {
                    if (value == 1) reply.Upvotes++;
                    if (value == -1) reply.Downvotes++;
                    var vote = new Vote
                    {
                        Id = voteId,
                        UserId = userId,
                        TargetType = VoteTargetType.Reply,
                        TargetId = replyId,
                        Value = value,
                        UpdatedAt = DateTime.UtcNow
                    };
                    if (existing is null) votes.Insert(vote);
                    else votes.Update(vote);
                }

                posts.Update(post);
                return post;
            });
        }
        finally
        {
            _postLock.Release();
        }
    }

    public IReadOnlyList<Figure> GetFigures(bool enabledOnly = false) => WithDb(db =>
    {
        IEnumerable<Figure> q = db.GetCollection<Figure>("figures").FindAll();
        if (enabledOnly)
            q = q.Where(f => f.Enabled);
        return q.ToList();
    });

    public Figure? GetFigure(string id) => WithDb(db =>
        db.GetCollection<Figure>("figures").FindById(id));

    public void UpsertFigure(Figure figure) => WithDb(db =>
    {
        var col = db.GetCollection<Figure>("figures");
        if (col.FindById(figure.Id) is not null)
            col.Update(figure);
        else
            col.Insert(figure);
    });

    public IReadOnlyList<PeerGroup> GetPeerGroups() => WithDb(db =>
        db.GetCollection<PeerGroup>("peerGroups").FindAll().ToList());

    public User? GetUser(string id) => WithDb(db =>
        db.GetCollection<User>("users").FindById(id));

    public void UpsertUser(User user) => WithDb(db =>
    {
        var col = db.GetCollection<User>("users");
        if (col.FindById(user.Id) is not null)
            col.Update(user);
        else
            col.Insert(user);
    });

    public void BanUser(string userId, string reason, string? ip)
    {
        var user = GetUser(userId) ?? new User { Id = userId, DisplayName = userId };
        user.IsBanned = true;
        user.BannedAt = DateTime.UtcNow;
        user.BanReason = reason;
        if (!string.IsNullOrEmpty(ip))
            user.LastIp = ip;
        UpsertUser(user);
    }

    public bool IsUserBanned(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return false;
        return GetUser(userId)?.IsBanned == true;
    }

    public void AddModerationLog(ModerationLog log) => WithDb(db =>
        db.GetCollection<ModerationLog>("moderationLogs").Insert(log));

    public FeedSettings GetSettings() => WithDb(db =>
        db.GetCollection<FeedSettings>("settings").FindById(FeedSettings.SingletonId) ?? new FeedSettings());

    public void SaveSettings(FeedSettings settings)
    {
        settings.Id = FeedSettings.SingletonId;
        WithDb(db =>
        {
            var col = db.GetCollection<FeedSettings>("settings");
            if (col.FindById(FeedSettings.SingletonId) is not null)
                col.Update(settings);
            else
                col.Insert(settings);
        });
    }

    public Vote? GetVote(string userId, VoteTargetType targetType, Guid targetId) => WithDb(db =>
    {
        var id = Vote.MakeId(userId, targetType, targetId);
        return db.GetCollection<Vote>("votes").FindById(id);
    });

    public int CountPosts() => WithDb(db => db.GetCollection<Post>("posts").Count());

    public int CountReplies() => WithDb(db =>
        db.GetCollection<Post>("posts").FindAll().Sum(p => p.Replies?.Count ?? 0));

    public void ClearFeed() => WithDb(db =>
    {
        db.GetCollection<Post>("posts").DeleteAll();
        db.GetCollection<Vote>("votes").DeleteAll();
    });

    public ExportBundle CreateExport() => WithDb(db => new ExportBundle
    {
        Version = ExportBundle.CurrentVersion,
        ExportedAt = DateTime.UtcNow,
        Posts = db.GetCollection<Post>("posts").FindAll().ToList(),
        Figures = db.GetCollection<Figure>("figures").FindAll().ToList(),
        PeerGroups = db.GetCollection<PeerGroup>("peerGroups").FindAll().ToList(),
        Users = db.GetCollection<User>("users").FindAll().ToList(),
        Settings = db.GetCollection<FeedSettings>("settings").FindById(FeedSettings.SingletonId)
    });

    public void RestoreExport(ExportBundle bundle)
    {
        if (bundle.Version != ExportBundle.CurrentVersion)
            throw new InvalidOperationException($"Unsupported export version {bundle.Version}; expected {ExportBundle.CurrentVersion}.");

        WithDb(db =>
        {
            db.GetCollection<Post>("posts").DeleteAll();
            db.GetCollection<Figure>("figures").DeleteAll();
            db.GetCollection<PeerGroup>("peerGroups").DeleteAll();
            db.GetCollection<User>("users").DeleteAll();
            db.GetCollection<Vote>("votes").DeleteAll();
            db.GetCollection<FeedSettings>("settings").DeleteAll();

            if (bundle.Posts.Count > 0)
                db.GetCollection<Post>("posts").InsertBulk(bundle.Posts);
            if (bundle.Figures.Count > 0)
                db.GetCollection<Figure>("figures").InsertBulk(bundle.Figures);
            if (bundle.PeerGroups.Count > 0)
                db.GetCollection<PeerGroup>("peerGroups").InsertBulk(bundle.PeerGroups);
            if (bundle.Users.Count > 0)
                db.GetCollection<User>("users").InsertBulk(bundle.Users);
            if (bundle.Settings is not null)
                db.GetCollection<FeedSettings>("settings").Insert(bundle.Settings);
            else
                db.GetCollection<FeedSettings>("settings").Insert(new FeedSettings());
        });
    }
}
