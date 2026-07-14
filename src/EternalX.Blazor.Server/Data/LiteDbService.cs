using LiteDB;
using EternalX.Blazor.Shared;
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
            var existing = figures.FindById(f.Id);
            if (existing is null)
            {
                figures.Insert(f);
                continue;
            }

            // Backfill self-picked @username without clobbering operator edits or persona.
            if (string.IsNullOrWhiteSpace(existing.Username) && !string.IsNullOrWhiteSpace(f.Username))
            {
                existing.Username = f.Username;
                figures.Update(existing);
            }
        }

        var settings = db.GetCollection<FeedSettings>("settings");
        if (settings.FindById(FeedSettings.SingletonId) is null)
            settings.Insert(new FeedSettings());
    }

    public IEnumerable<Post> GetRecentPosts(int count = 50) => WithDb(db =>
    {
        var figures = db.GetCollection<Figure>("figures").FindAll().ToDictionary(f => f.Id, StringComparer.OrdinalIgnoreCase);
        return db.GetCollection<Post>("posts")
            .FindAll()
            .OrderByDescending(p => p.CreatedAt)
            .Take(count)
            .Select(p => HydrateAuthors(p, figures))
            .ToList();
    });

    /// <summary>
    /// Posts that reference a hashtag in body, stored tags, quote, or replies.
    /// Scans a wider window than the home timeline so tag pages stay useful.
    /// </summary>
    public IReadOnlyList<Post> GetPostsByHashtag(string tag, int count = 50) => WithDb(db =>
    {
        var figures = db.GetCollection<Figure>("figures").FindAll().ToDictionary(f => f.Id, StringComparer.OrdinalIgnoreCase);
        var all = db.GetCollection<Post>("posts").FindAll().ToList();
        var matched = ContentTags.FilterByHashtag(all, tag, take: count <= 0 ? 50 : count);
        return matched.Select(p => HydrateAuthors(p, figures)).ToList();
    });

    public Post? GetPost(Guid id) => WithDb(db =>
    {
        var post = db.GetCollection<Post>("posts").FindById(id);
        if (post is null) return null;
        var figures = db.GetCollection<Figure>("figures").FindAll().ToDictionary(f => f.Id, StringComparer.OrdinalIgnoreCase);
        return HydrateAuthors(post, figures);
    });

    /// <summary>
    /// Ensures AI posts/replies expose real name + username for the X meta line,
    /// including legacy rows that only stored @@handle in Author.
    /// </summary>
    internal static Post HydrateAuthors(Post post, IReadOnlyDictionary<string, Figure> figures)
    {
        HydrateOne(post, figures);
        if (post.Replies is { Count: > 0 })
        {
            foreach (var reply in post.Replies)
                HydrateOne(reply, figures);
        }

        if (post.QuotedIsAi &&
            string.IsNullOrWhiteSpace(post.QuotedAuthorUsername) &&
            !string.IsNullOrWhiteSpace(post.QuotedAuthor) &&
            post.QuotedAuthor.TrimStart().StartsWith('@'))
        {
            post.QuotedAuthorUsername = FigureHandles.Normalize(post.QuotedAuthor);
        }

        return post;
    }

    private static void HydrateOne(Post post, IReadOnlyDictionary<string, Figure> figures)
    {
        if (!post.IsAi || string.IsNullOrWhiteSpace(post.FigureId))
            return;
        if (!figures.TryGetValue(post.FigureId, out var fig))
            return;

        if (string.IsNullOrWhiteSpace(post.AuthorUsername))
            post.AuthorUsername = fig.ResolvedUsername;

        // Legacy: Author was only the @handle. Prefer roster real name.
        if (string.IsNullOrWhiteSpace(post.Author) || post.Author.TrimStart().StartsWith('@'))
            post.Author = fig.Name;
    }

    private static void HydrateOne(Reply reply, IReadOnlyDictionary<string, Figure> figures)
    {
        if (!reply.IsAi || string.IsNullOrWhiteSpace(reply.FigureId))
            return;
        if (!figures.TryGetValue(reply.FigureId, out var fig))
            return;

        if (string.IsNullOrWhiteSpace(reply.AuthorUsername))
            reply.AuthorUsername = fig.ResolvedUsername;

        if (string.IsNullOrWhiteSpace(reply.Author) || reply.Author.TrimStart().StartsWith('@'))
            reply.Author = fig.Name;
    }

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

    /// <summary>
    /// Toggle X-style like (heart). Only +1 / 0; downvotes are not used.
    /// Returns (post, likedByUser).
    /// </summary>
    public async Task<(Post? Post, bool Liked)> TogglePostLikeAsync(
        string userId,
        Guid postId,
        CancellationToken cancellationToken = default)
    {
        await _postLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return WithDb(db =>
            {
                var posts = db.GetCollection<Post>("posts");
                var votes = db.GetCollection<Vote>("votes");
                var post = posts.FindById(postId);
                if (post is null)
                    return ((Post?)null, false);

                var voteId = Vote.MakeId(userId, VoteTargetType.Post, postId);
                var existing = votes.FindById(voteId);
                var wasLiked = existing is { Value: 1 };

                if (wasLiked)
                {
                    // Unlike
                    if (existing!.Value == -1)
                        post.Downvotes = Math.Max(0, post.Downvotes - 1);
                    post.Upvotes = Math.Max(0, post.Upvotes - 1);
                    votes.Delete(voteId);
                    posts.Update(post);
                    return (post, false);
                }

                // Like (clear any legacy downvote)
                if (existing is { Value: -1 })
                {
                    post.Downvotes = Math.Max(0, post.Downvotes - 1);
                    votes.Delete(voteId);
                    existing = null;
                }

                post.Upvotes++;
                var vote = new Vote
                {
                    Id = voteId,
                    UserId = userId,
                    TargetType = VoteTargetType.Post,
                    TargetId = postId,
                    Value = 1,
                    UpdatedAt = DateTime.UtcNow
                };
                if (existing is null) votes.Insert(vote);
                else votes.Update(vote);

                posts.Update(post);
                return (post, true);
            });
        }
        finally
        {
            _postLock.Release();
        }
    }

    public async Task<(Post? Post, bool Liked)> ToggleReplyLikeAsync(
        string userId,
        Guid postId,
        Guid replyId,
        CancellationToken cancellationToken = default)
    {
        await _postLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return WithDb(db =>
            {
                var posts = db.GetCollection<Post>("posts");
                var votes = db.GetCollection<Vote>("votes");
                var post = posts.FindById(postId);
                if (post is null)
                    return ((Post?)null, false);

                var reply = post.Replies?.FirstOrDefault(r => r.Id == replyId);
                if (reply is null)
                    return ((Post?)null, false);

                var voteId = Vote.MakeId(userId, VoteTargetType.Reply, replyId);
                var existing = votes.FindById(voteId);
                var wasLiked = existing is { Value: 1 };

                if (wasLiked)
                {
                    if (existing!.Value == -1)
                        reply.Downvotes = Math.Max(0, reply.Downvotes - 1);
                    reply.Upvotes = Math.Max(0, reply.Upvotes - 1);
                    votes.Delete(voteId);
                    posts.Update(post);
                    return (post, false);
                }

                if (existing is { Value: -1 })
                {
                    reply.Downvotes = Math.Max(0, reply.Downvotes - 1);
                    votes.Delete(voteId);
                    existing = null;
                }

                reply.Upvotes++;
                var vote = new Vote
                {
                    Id = voteId,
                    UserId = userId,
                    TargetType = VoteTargetType.Reply,
                    TargetId = replyId,
                    Value = 1,
                    UpdatedAt = DateTime.UtcNow
                };
                if (existing is null) votes.Insert(vote);
                else votes.Update(vote);

                posts.Update(post);
                return (post, true);
            });
        }
        finally
        {
            _postLock.Release();
        }
    }

    /// <summary>
    /// Create a new timeline post that quotes another (X-style reshare with optional comment).
    /// Not a reply: appears as its own post and increments the source ReshareCount.
    /// </summary>
    public async Task<Post?> CreateQuoteReshareAsync(
        Guid sourcePostId,
        Post quotePost,
        CancellationToken cancellationToken = default)
    {
        await _postLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return WithDb(db =>
            {
                var posts = db.GetCollection<Post>("posts");
                var source = posts.FindById(sourcePostId);
                if (source is null)
                    return null;

                quotePost.QuotedPostId = source.Id;
                quotePost.QuotedAuthor = source.Author;
                quotePost.QuotedAuthorUsername = source.AuthorUsername;
                quotePost.QuotedContent = source.Content;
                quotePost.QuotedIsAi = source.IsAi;
                quotePost.Mentions = ContentTags.ExtractMentions(quotePost.Content);
                quotePost.Hashtags = ContentTags.ExtractHashtags(quotePost.Content);

                posts.Insert(quotePost);

                source.ShareCount++;
                posts.Update(source);
                return quotePost;
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
