using EternalX.Blazor.Server.Services;
using Microsoft.Extensions.Configuration;

namespace EternalX.Blazor.Server.Tests.Services;

public class AiServiceTests
{
    [Fact]
    public async Task GenerateReplyAsync_does_not_echo_full_prompt()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var ai = new AiService(config, new HttpClient());
        var huge = new string('z', 20_000);

        var reply = await ai.GenerateReplyAsync(huge, "claude");

        Assert.DoesNotContain(huge, reply);
        Assert.True(reply.Length < 200, $"reply length {reply.Length}");
        Assert.Contains("[CLAUDE]", reply);
    }
}
