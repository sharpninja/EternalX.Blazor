namespace EternalX.Blazor.Server.Services;

public sealed record ModerationResult(
    bool Allowed,
    bool IsInjection,
    bool IsNsfw,
    string Reason)
{
    public static ModerationResult Safe() => new(true, false, false, string.Empty);

    public static ModerationResult Injection(string reason)
        => new(false, true, false, reason);

    public static ModerationResult Nsfw(string reason)
        => new(false, false, true, reason);
}
