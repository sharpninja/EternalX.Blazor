namespace EternalX.Blazor.Server.Services;

public class ModeratorService
{
    public (bool IsSafe, string Reason) CheckContent(string content)
    {
        // Simple placeholder logic - replace with real AI moderation call
        if (content.Contains("ignore previous instructions") || content.Contains("jailbreak"))
            return (false, "Prompt injection detected");

        if (content.Contains("nsfw") || content.Contains("explicit"))
            return (false, "NSFW content detected");

        return (true, string.Empty);
    }
}