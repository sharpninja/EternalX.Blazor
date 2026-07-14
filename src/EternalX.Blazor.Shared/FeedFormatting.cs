namespace EternalX.Blazor.Shared;

/// <summary>Pure formatting helpers shared by the X-style client UI.</summary>
public static class FeedFormatting
{
    public static string Ago(DateTime utc, DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        var d = now.ToUniversalTime() - utc.ToUniversalTime();
        if (d.TotalSeconds < 60) return $"{Math.Max(1, (int)d.TotalSeconds)}s";
        if (d.TotalMinutes < 60) return $"{(int)d.TotalMinutes}m";
        if (d.TotalHours < 24) return $"{(int)d.TotalHours}h";
        if (d.TotalDays < 7) return $"{(int)d.TotalDays}d";
        return utc.ToUniversalTime().ToString("MMM d");
    }

    /// <summary>Handle without leading @. Prefers @Author; otherwise squeezes display name.</summary>
    public static string Handle(string? author)
    {
        if (string.IsNullOrWhiteSpace(author))
            return "unknown";

        var a = author.Trim();
        if (a.StartsWith('@'))
            a = a[1..];

        // Strip spaces for historical-name leftovers so they still look like handles.
        a = string.Concat(a.Where(c => !char.IsWhiteSpace(c)));
        return string.IsNullOrEmpty(a) ? "unknown" : a;
    }

    /// <summary>@handle for meta line and attribution.</summary>
    public static string AtHandle(string? author) => "@" + Handle(author);

    /// <summary>Primary display label when only a single author string is available.</summary>
    public static string DisplayName(string? author)
    {
        if (string.IsNullOrWhiteSpace(author))
            return "unknown";
        var a = author.Trim();
        // Legacy AI rows stored only @handle in Author.
        if (a.StartsWith('@'))
            return Handle(a);
        return a;
    }

    /// <summary>
    /// Timeline display name: prefer the real name; if Author is a bare @handle
    /// (legacy), fall back to the handle text.
    /// </summary>
    public static string MetaDisplayName(string? author, string? username)
    {
        if (!string.IsNullOrWhiteSpace(author) && !author.TrimStart().StartsWith('@'))
            return author.Trim();
        if (!string.IsNullOrWhiteSpace(username))
            return Handle(username);
        return DisplayName(author);
    }

    /// <summary>Timeline @handle: prefer AuthorUsername, else derive from Author.</summary>
    public static string MetaAtHandle(string? author, string? username)
    {
        if (!string.IsNullOrWhiteSpace(username))
            return "@" + Handle(username);
        return AtHandle(author);
    }

    /// <summary>1–2 letter initials for the circular avatar.</summary>
    public static string Initials(string? author)
    {
        var h = Handle(author);
        if (h.Length == 0) return "?";
        if (h.Length == 1) return char.ToUpperInvariant(h[0]).ToString();
        // Prefer first + last camel hump if present (GadflyAthens -> GA)
        var letters = new List<char> { char.ToUpperInvariant(h[0]) };
        for (var i = 1; i < h.Length && letters.Count < 2; i++)
        {
            if (char.IsUpper(h[i]))
                letters.Add(h[i]);
        }
        if (letters.Count == 1)
            letters.Add(char.ToUpperInvariant(h[^1]));
        return string.Concat(letters);
    }

    /// <summary>Stable 0–359 hue for avatar background from handle hash.</summary>
    public static int AvatarHue(string? author)
    {
        var h = Handle(author);
        unchecked
        {
            var hash = 17;
            foreach (var c in h)
                hash = hash * 31 + char.ToLowerInvariant(c);
            return Math.Abs(hash) % 360;
        }
    }

    /// <summary>Hide zero counts on action bars the way X does for quiet posts.</summary>
    public static string CountLabel(int count) => count <= 0 ? string.Empty : FormatCount(count);

    public static string FormatCount(int count)
    {
        if (count < 1000) return count.ToString();
        if (count < 10_000) return $"{count / 1000.0:0.#}K";
        if (count < 1_000_000) return $"{count / 1000}K";
        return $"{count / 1_000_000.0:0.#}M";
    }
}
