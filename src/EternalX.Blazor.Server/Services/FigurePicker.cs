using EternalX.Blazor.Shared.Models;

namespace EternalX.Blazor.Server.Services;

/// <summary>
/// Server-side figure assignment (FR-AI-006/007). Models never choose identity.
/// </summary>
public static class FigurePicker
{
    public static Figure? Pick(
        IReadOnlyList<Figure> roster,
        IReadOnlyCollection<string>? allowedPeerGroupIds = null,
        IReadOnlyCollection<string>? recentFigureIds = null,
        Random? random = null)
    {
        random ??= Random.Shared;
        var enabled = roster.Where(f => f.Enabled).ToList();
        if (enabled.Count == 0)
            return null;

        IEnumerable<Figure> pool = enabled;
        if (allowedPeerGroupIds is { Count: > 0 })
        {
            var filtered = enabled
                .Where(f => f.PeerGroupIds.Any(g => allowedPeerGroupIds.Contains(g)))
                .ToList();
            if (filtered.Count > 0)
                pool = filtered;
            // else fall back to all enabled so the feed never dead-ends
        }

        var candidates = pool.ToList();

        // Prefer figures that share a peer group with recent speakers (crossover).
        if (recentFigureIds is { Count: > 0 })
        {
            var recentGroups = enabled
                .Where(f => recentFigureIds.Contains(f.Id))
                .SelectMany(f => f.PeerGroupIds)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (recentGroups.Count > 0)
            {
                var peers = candidates
                    .Where(f => f.PeerGroupIds.Any(g => recentGroups.Contains(g)))
                    .Where(f => !recentFigureIds.Contains(f.Id))
                    .ToList();
                if (peers.Count > 0)
                    candidates = peers;
            }
        }

        return candidates[random.Next(candidates.Count)];
    }
}
