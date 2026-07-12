namespace EternalX.Blazor.Shared.Models;

public class User
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty; // google, microsoft, github
    public bool IsBanned { get; set; }
}