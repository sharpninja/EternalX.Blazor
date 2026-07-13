namespace EternalX.Blazor.Server.Tests.Deploy;

/// <summary>TEST-CORE-003: deploy script must inject shared EternalReadit AI key names.</summary>
public class OctopusDeployScriptTests
{
    private static string ScriptPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "deploy", "octopus-deploy.ps1");
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "deploy", "octopus-deploy.ps1");
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate deploy/octopus-deploy.ps1");
    }

    [Fact]
    public void Script_passes_shared_claude_and_grok_octopus_keys()
    {
        var text = File.ReadAllText(ScriptPath());

        // Same canonical names as EternalReadit Octopus library/project variables.
        Assert.Contains("ANTHROPIC_API_KEY", text);
        Assert.Contains("XAI_API_KEY", text);
        Assert.Contains("OPENAI_API_KEY", text);
        Assert.Contains("HF_API_KEY", text);
        Assert.Contains("GATEWAY_KEY", text);

        // Env-file pattern (avoids leaking keys on the process command line).
        Assert.Contains("--env-file", text);
        Assert.Contains("eternalx.env", text);
        Assert.Contains("OctopusParameters", text);
    }
}
