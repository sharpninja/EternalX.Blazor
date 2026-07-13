using EternalX.Blazor.Server.Services;
using EternalX.Blazor.Shared.Models;

namespace EternalX.Blazor.Server.Tests.Services;

/// <summary>TEST-AI-001 figure pick and peer-group fallback.</summary>
public class FigurePickerTests
{
    private static List<Figure> Roster() =>
    [
        new() { Id = "a", Name = "A", Persona = "pa", Enabled = true, PeerGroupIds = ["g1"] },
        new() { Id = "b", Name = "B", Persona = "pb", Enabled = true, PeerGroupIds = ["g2"] },
        new() { Id = "c", Name = "C", Persona = "pc", Enabled = false, PeerGroupIds = ["g1"] },
        new() { Id = "d", Name = "D", Persona = "pd", Enabled = true, PeerGroupIds = ["g1", "g2"] },
    ];

    [Fact]
    public void Pick_ignores_disabled()
    {
        var picks = Enumerable.Range(0, 30)
            .Select(_ => FigurePicker.Pick(Roster(), random: new Random(1)))
            .Where(f => f is not null)
            .Select(f => f!.Id)
            .ToHashSet();

        Assert.DoesNotContain("c", picks);
        Assert.Contains("a", picks);
    }

    [Fact]
    public void Empty_allowlist_does_not_dead_end()
    {
        var fig = FigurePicker.Pick(Roster(), allowedPeerGroupIds: Array.Empty<string>(), random: new Random(2));
        Assert.NotNull(fig);
    }

    [Fact]
    public void Unknown_allowlist_falls_back_to_all_enabled()
    {
        var fig = FigurePicker.Pick(Roster(), allowedPeerGroupIds: ["missing-group"], random: new Random(3));
        Assert.NotNull(fig);
        Assert.True(fig!.Enabled);
    }

    [Fact]
    public void Allowlist_filters_when_matches_exist()
    {
        for (var i = 0; i < 20; i++)
        {
            var fig = FigurePicker.Pick(Roster(), allowedPeerGroupIds: ["g2"], random: new Random(i));
            Assert.NotNull(fig);
            Assert.Contains("g2", fig!.PeerGroupIds);
        }
    }
}
