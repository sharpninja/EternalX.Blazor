namespace EternalX.Blazor.Server.Services;

/// <param name="Username">Self-picked @handle without leading @; preferred public identity.</param>
public sealed record AiRequest(
    string Persona,
    string FigureName,
    string UserPrompt,
    string? Model = null,
    string? Effort = null,
    string? Username = null);

public sealed record AiResult(string Text, string Provider, string Model);

public interface IAiProvider
{
    string Name { get; }
    bool IsConfigured { get; }
    Task<AiResult> GenerateAsync(AiRequest request, CancellationToken cancellationToken = default);
}
