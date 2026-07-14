using System.Text.RegularExpressions;

namespace EternalX.Blazor.Shared.Models;

/// <summary>
/// X-style @handles for historical personalities. Usernames are self-picked
/// style handles (not formal historical names) used as feed authors.
/// </summary>
public static partial class FigureHandles
{
    /// <summary>Strip leading @, keep letters/digits/underscore only, max 30.</summary>
    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var s = raw.Trim();
        if (s.StartsWith('@'))
            s = s[1..];

        s = InvalidUsernameChars().Replace(s, string.Empty);
        if (s.Length > 30)
            s = s[..30];
        return s;
    }

    /// <summary>Fallback when Username is empty: squeeze historical name into a handle.</summary>
    public static string FromDisplayName(string? name)
        => Normalize(string.Concat((name ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries)));

    /// <summary>Handle without @; prefers stored Username, else name-derived fallback.</summary>
    public static string ResolveUsername(string? username, string? displayName)
    {
        var fromUser = Normalize(username);
        if (!string.IsNullOrEmpty(fromUser))
            return fromUser;
        return FromDisplayName(displayName);
    }

    /// <summary>Timeline author string: "@Handle".</summary>
    public static string AtHandle(string? username, string? displayName)
    {
        var u = ResolveUsername(username, displayName);
        return string.IsNullOrEmpty(u) ? (displayName ?? "unknown") : "@" + u;
    }

    [GeneratedRegex(@"[^A-Za-z0-9_]", RegexOptions.CultureInvariant)]
    private static partial Regex InvalidUsernameChars();
}
