namespace EternalX.Blazor.Server.Services;

/// <summary>Deterministic offline provider used when API keys are absent.</summary>
public sealed class StubAiProvider : IAiProvider
{
    public string Name => "stub";
    public bool IsConfigured => true;

    public Task<AiResult> GenerateAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        // Never echo the full user prompt (production auto-reply incident).
        var snippet = request.UserPrompt.Length <= 40
            ? request.UserPrompt
            : request.UserPrompt[..40] + "...";

        var text =
            $"{request.FigureName}: In character — {snippet} " +
            $"(stub; persona notes: {Truncate(request.Persona, 60)})";

        return Task.FromResult(new AiResult(text, Name, "stub-1"));
    }

    private static string Truncate(string value, int max)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= max)
            return value ?? string.Empty;
        return value[..(max - 3)] + "...";
    }
}
