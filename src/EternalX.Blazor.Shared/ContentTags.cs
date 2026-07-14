using System.Text.RegularExpressions;
using EternalX.Blazor.Shared.Models;

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

    /// <summary>Strip leading # and invalid chars; empty if not a usable tag.</summary>
    public static string NormalizeHashtag(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var s = raw.Trim();
        if (s.StartsWith('#'))
            s = s[1..];

        s = new string(s.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
        if (s.Length > 50)
            s = s[..50];
        return s;
    }

    /// <summary>True if post body, stored tags, quote body, or any reply references the tag.</summary>
    public static bool PostMatchesHashtag(Post post, string tag)
    {
        var normalized = NormalizeHashtag(tag);
        if (string.IsNullOrEmpty(normalized) || post is null)
            return false;

        if (ListHasTag(post.Hashtags, normalized))
            return true;
        if (ContentHasTag(post.Content, normalized))
            return true;
        if (ContentHasTag(post.QuotedContent, normalized))
            return true;

        foreach (var reply in post.Replies ?? Enumerable.Empty<Reply>())
        {
            if (ListHasTag(reply.Hashtags, normalized))
                return true;
            if (ContentHasTag(reply.Content, normalized))
                return true;
        }

        return false;
    }

    /// <summary>Filter and order posts for a hashtag timeline (newest first).</summary>
    public static IReadOnlyList<Post> FilterByHashtag(IEnumerable<Post> posts, string tag, int take = 50)
    {
        var normalized = NormalizeHashtag(tag);
        if (string.IsNullOrEmpty(normalized))
            return Array.Empty<Post>();

        take = take <= 0 ? 50 : take;
        return posts
            .Where(p => PostMatchesHashtag(p, normalized))
            .OrderByDescending(p => p.CreatedAt)
            .Take(take)
            .ToList();
    }

    private static bool ListHasTag(IEnumerable<string>? tags, string normalized) =>
        tags is not null &&
        tags.Any(t => string.Equals(NormalizeHashtag(t), normalized, StringComparison.OrdinalIgnoreCase));

    private static bool ContentHasTag(string? content, string normalized) =>
        ExtractHashtags(content).Any(t => string.Equals(t, normalized, StringComparison.OrdinalIgnoreCase));

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
