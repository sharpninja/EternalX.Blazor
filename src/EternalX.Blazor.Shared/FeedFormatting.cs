namespace EternalX.Blazor.Shared;

/// <summary>Pure formatting helpers shared by the X-style client UI.</summary>
public static class FeedFormatting
{
    public static string Ago(DateTime utc, DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        var d = now.ToUniversalTime() - utc.ToUniversalTime();
        if (d.TotalMinutes < 60) return $"{(int)Math.Max(1, d.TotalMinutes)}m";
        if (d.TotalHours < 24) return $"{(int)d.TotalHours}h";
        return $"{(int)d.TotalDays}d";
    }
}
