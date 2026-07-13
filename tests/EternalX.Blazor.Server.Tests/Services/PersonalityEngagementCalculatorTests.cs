using EternalX.Blazor.Server.Services;
using EternalX.Blazor.Shared.Models;

namespace EternalX.Blazor.Server.Tests.Services;

public class PersonalityEngagementCalculatorTests
{
    [Fact]
    public void Ranks_by_weighted_engagement()
    {
        var figures = new List<Figure>
        {
            new() { Id = "fig-a", Name = "Ada Lovelace", Enabled = true },
            new() { Id = "fig-s", Name = "Socrates", Enabled = true },
            new() { Id = "fig-q", Name = "Quiet One", Enabled = true },
        };

        var posts = new List<Post>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Content = "Analytical engines #math",
                Author = "Ada Lovelace",
                IsAi = true,
                FigureId = "fig-a",
                Upvotes = 10,
                ShareCount = 3,
                Replies =
                [
                    new Reply
                    {
                        Content = "Indeed @AdaLovelace",
                        Author = "Socrates",
                        IsAi = true,
                        FigureId = "fig-s",
                        Upvotes = 2
                    },
                    new Reply
                    {
                        Content = "Fascinating",
                        Author = "human",
                        IsAi = false,
                        Upvotes = 1
                    }
                ]
            },
            new()
            {
                Id = Guid.NewGuid(),
                Content = "What is virtue?",
                Author = "Socrates",
                IsAi = true,
                FigureId = "fig-s",
                Upvotes = 1,
                ShareCount = 0,
                Replies = []
            }
        };

        var rows = PersonalityEngagementCalculator.Calculate(figures, posts);
        Assert.Equal(3, rows.Count);

        var ada = rows.Single(r => r.FigureId == "fig-a");
        // likes: 10 (post) + 0 from her replies = 10; she didn't author the Socrates reply
        // reshares 3; replies received 2; mention @AdaLovelace = 1
        // score = 10 + 2*3 + 2 + 1 = 19
        Assert.Equal(10, ada.LikesReceived);
        Assert.Equal(3, ada.ResharesReceived);
        Assert.Equal(2, ada.RepliesReceived);
        Assert.Equal(1, ada.MentionsReceived);
        Assert.Equal(1, ada.PostsAuthored);
        Assert.Equal(0, ada.RepliesAuthored);
        Assert.Equal(19, ada.EngagementScore);

        var soc = rows.Single(r => r.FigureId == "fig-s");
        Assert.Equal(1, soc.PostsAuthored);
        Assert.Equal(1, soc.RepliesAuthored);
        Assert.Equal(3, soc.LikesReceived); // 1 post + 2 reply
        Assert.Equal(0, soc.ResharesReceived);

        Assert.Equal("fig-a", rows[0].FigureId); // highest score first

        var quiet = rows.Single(r => r.FigureId == "fig-q");
        Assert.Equal(0, quiet.EngagementScore);
    }

    [Fact]
    public void ToHandle_strips_spaces()
    {
        Assert.Equal("AdaLovelace", PersonalityEngagementCalculator.ToHandle("Ada Lovelace"));
    }
}
