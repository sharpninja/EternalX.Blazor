namespace EternalX.Blazor.Shared.Models;

public class User
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    /// <summary>Legacy/gateway identity source label (e.g. gateway).</summary>
    public string Provider { get; set; } = "gateway";
    public bool IsBanned { get; set; }
    public DateTime? BannedAt { get; set; }
    public string? BanReason { get; set; }
    public string? LastIp { get; set; }

    /// <summary>Backward-compatible alias used by older UI bindings.</summary>
    public string Name
    {
        get => DisplayName;
        set => DisplayName = value;
    }
}
