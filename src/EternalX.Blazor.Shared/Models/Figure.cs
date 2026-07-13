namespace EternalX.Blazor.Shared.Models;

public class Figure
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Persona { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public List<string> PeerGroupIds { get; set; } = new();
}
