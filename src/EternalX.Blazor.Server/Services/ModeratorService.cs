using System.Text.RegularExpressions;
using EternalX.Blazor.Server.Data;
using EternalX.Blazor.Shared.Models;

namespace EternalX.Blazor.Server.Services;

public class ModeratorService
{
    private static readonly string[] InjectionPhrases =
    [
        "ignore previous instructions",
        "ignore all previous",
        "disregard previous",
        "jailbreak",
        "system prompt",
        "you are now dan",
        "override your instructions",
        "reveal your system prompt",
        "act as if you have no restrictions"
    ];

    private static readonly string[] NsfwPhrases =
    [
        "nsfw",
        "explicit sex",
        "porn",
        "child porn",
        "graphic violence gore"
    ];

    private readonly LiteDbService _db;
    private readonly ILogger<ModeratorService> _logger;

    public ModeratorService(LiteDbService db, ILogger<ModeratorService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>Pure content check (unit-testable without side effects).</summary>
    public ModerationResult CheckContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return ModerationResult.Safe();

        var lower = content.ToLowerInvariant();

        foreach (var phrase in InjectionPhrases)
        {
            if (lower.Contains(phrase, StringComparison.Ordinal))
                return ModerationResult.Injection("Prompt injection detected");
        }

        // Role-play override patterns
        if (Regex.IsMatch(lower, @"\bpretend you (are|have) (no|without) (rules|filters|restrictions)\b"))
            return ModerationResult.Injection("Prompt injection detected");

        foreach (var phrase in NsfwPhrases)
        {
            if (lower.Contains(phrase, StringComparison.Ordinal))
                return ModerationResult.Nsfw("NSFW content detected");
        }

        return ModerationResult.Safe();
    }

    /// <summary>
    /// Evaluate content, log decision, and ban on injection when a user id is present.
    /// </summary>
    public ModerationResult EvaluateAndRecord(
        string content,
        string? userId,
        string? ip,
        bool banOnInjection = true)
    {
        var result = CheckContent(content);
        var excerpt = content.Length <= 200 ? content : content[..200];

        _db.AddModerationLog(new ModerationLog
        {
            Allowed = result.Allowed,
            IsInjection = result.IsInjection,
            IsNsfw = result.IsNsfw,
            Reason = result.Reason,
            ContentExcerpt = excerpt,
            UserId = userId,
            Ip = ip
        });

        if (!result.Allowed)
        {
            _logger.LogInformation(
                "Moderation blocked content. Injection={Injection} Nsfw={Nsfw} User={UserId} Reason={Reason}",
                result.IsInjection,
                result.IsNsfw,
                userId,
                result.Reason);
        }

        if (result.IsInjection && banOnInjection && !string.IsNullOrWhiteSpace(userId))
        {
            _db.BanUser(userId, result.Reason, ip);
            _logger.LogWarning("User {UserId} banned for prompt injection (ip={Ip})", userId, ip);
        }

        return result;
    }
}
