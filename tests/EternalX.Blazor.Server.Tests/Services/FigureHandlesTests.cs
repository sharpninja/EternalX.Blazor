using EternalX.Blazor.Server.Data;
using EternalX.Blazor.Shared.Models;

namespace EternalX.Blazor.Server.Tests.Services;

public class FigureHandlesTests
{
    [Theory]
    [InlineData("@GadflyAthens", "GadflyAthens")]
    [InlineData("  WirelessDreamer  ", "WirelessDreamer")]
    [InlineData("bad-name!", "badname")]
    public void Normalize_strips_at_and_invalid_chars(string raw, string expected)
        => Assert.Equal(expected, FigureHandles.Normalize(raw));

    [Fact]
    public void AtHandle_prefers_username_over_display_name()
    {
        Assert.Equal("@AnalyticalPoet", FigureHandles.AtHandle("AnalyticalPoet", "Ada Lovelace"));
        Assert.Equal("@AdaLovelace", FigureHandles.AtHandle(null, "Ada Lovelace"));
    }

    [Fact]
    public void Figure_AtHandle_uses_self_picked_username()
    {
        var fig = new Figure { Name = "Socrates", Username = "GadflyAthens" };
        Assert.Equal("GadflyAthens", fig.ResolvedUsername);
        Assert.Equal("@GadflyAthens", fig.AtHandle);
    }

    [Fact]
    public void DefaultRoster_every_figure_has_unique_username()
    {
        var figures = DefaultRoster.Figures();
        Assert.Equal(46, figures.Count);
        Assert.All(figures, f =>
        {
            Assert.False(string.IsNullOrWhiteSpace(f.Username), f.Name);
            Assert.Equal(FigureHandles.Normalize(f.Username), f.Username);
            Assert.Matches("^[A-Za-z0-9_]{1,30}$", f.Username);
            Assert.StartsWith("@", f.AtHandle);
            Assert.DoesNotContain(' ', f.AtHandle);
            Assert.False(string.IsNullOrWhiteSpace(f.Id), f.Name);
            Assert.StartsWith("fig-", f.Id);
        });

        var handles = figures.Select(f => f.Username).ToList();
        Assert.Equal(handles.Count, handles.Distinct(StringComparer.OrdinalIgnoreCase).Count());

        var ids = figures.Select(f => f.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }
}
