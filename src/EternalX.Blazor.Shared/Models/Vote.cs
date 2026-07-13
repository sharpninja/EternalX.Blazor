namespace EternalX.Blazor.Shared.Models;

public enum VoteTargetType
{
    Post = 0,
    Reply = 1
}

public class Vote
{
    /// <summary>Composite key: {userId}:{targetType}:{targetId}</summary>
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public VoteTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
    /// <summary>+1, -1, or 0 (cleared).</summary>
    public int Value { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public static string MakeId(string userId, VoteTargetType targetType, Guid targetId)
        => $"{userId}:{(int)targetType}:{targetId:N}";
}
