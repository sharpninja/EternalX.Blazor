using System.Text.Json;

namespace EternalX.Blazor.Server.Services;

/// <summary>Pure JSON extractors for live provider responses (unit-tested).</summary>
public static class AiResponseParsers
{
    public static string ParseClaude(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString()?.Trim()
               ?? string.Empty;
    }

    public static string ParseOpenAiCompatible(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content")
                   .GetString()?.Trim()
               ?? string.Empty;
    }

    public static string ParseHuggingFace(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
            {
                var first = doc.RootElement[0];
                if (first.TryGetProperty("generated_text", out var gt))
                    return (gt.GetString() ?? raw).Trim();
            }

            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("generated_text", out var single))
                return (single.GetString() ?? raw).Trim();
        }
        catch
        {
            // fall through
        }

        return raw.Trim();
    }
}
