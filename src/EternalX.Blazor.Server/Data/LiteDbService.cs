using LiteDB;
using EternalX.Blazor.Shared.Models;

namespace EternalX.Blazor.Server.Data;

public class LiteDbService
{
    private readonly string _dbPath;

    public LiteDbService(IConfiguration config)
    {
        _dbPath = config["LITEDB_PATH"] ?? "/app/data/eternalx.db";
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
    }

    public LiteDatabase GetDatabase() => new LiteDatabase(_dbPath);

    public IEnumerable<Post> GetRecentPosts(int count = 50)
    {
        using var db = GetDatabase();
        return db.GetCollection<Post>("posts")
            .FindAll()
            .OrderByDescending(p => p.CreatedAt)
            .Take(count)
            .ToList();
    }

    public void SavePost(Post post)
    {
        using var db = GetDatabase();
        db.GetCollection<Post>("posts").Insert(post);
    }

    public void UpdatePost(Post post)
    {
        using var db = GetDatabase();
        db.GetCollection<Post>("posts").Update(post);
    }
}