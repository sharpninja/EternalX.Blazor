using System.Text.RegularExpressions;

namespace EternalX.Blazor.Shared;

/// <summary>X-style @mentions and #hashtags parsing/rendering helpers.</summary>
public static class ContentTags
{
    // @Handle: letters, numbers, underscore; 1-30 after @
    private static readonly Regex MentionRegex = new(
        @"(?<![A-Za-z0-9_])@([A-Za-z0-9_]{1,30})",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // #topic: letters, numbers, underscore; 1-50 after #
    private static readonly Regex HashtagRegex = new(
        @"(?<![A-Za-z0-9_])#([A-Za-z0-9_]{1,50})",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex TokenRegex = new(
        @"(@[A-Za-z0-9_]{1,30})|(#[A-Za-z0-9_]{1,50})",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static List<string> ExtractMentions(string? content)
    {
        if (string.IsNullOrEmpty(content))
            return new List<string>();

        return MentionRegex.Matches(content)
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static List<string> ExtractHashtags(string? content)
    {
        if (string.IsNullOrEmpty(content))
            return new List<string>();

        return HashtagRegex.Matches(content)
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public enum SegmentKind { Text, Mention, Hashtag }

    public sealed record Segment(SegmentKind Kind, string Text);

    /// <summary>Split content into plain / @mention / #hashtag segments for UI markup.</summary>
    public static IReadOnlyList<Segment> Tokenize(string? content)
    {
        if (string.IsNullOrEmpty(content))
            return Array.Empty<Segment>();

        var segments = new List<Segment>();
        var last = 0;
        foreach (Match m in TokenRegex.Matches(content))
        {
            if (m.Index > last)
                segments.Add(new Segment(SegmentKind.Text, content[last..m.Index]));

            var token = m.Value;
            if (token.StartsWith('@'))
                segments.Add(new Segment(SegmentKind.Mention, token));
            else
                segments.Add(new Segment(SegmentKind.Hashtag, token));

            last = m.Index + m.Length;
        }

        if (last < content.Length)
            segments.Add(new Segment(SegmentKind.Text, content[last..]));

        return segments;
    }
}
