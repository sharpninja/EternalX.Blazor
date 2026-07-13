using EternalX.Blazor.Shared.Models;

namespace EternalX.Blazor.Server.Data;

/// <summary>
/// Immutable default seed data (TR-AI-SEED-001). Callers must treat returned
/// collections as fresh snapshots; seed inserts per-id if absent only.
/// </summary>
public static class DefaultRoster
{
    public static IReadOnlyList<PeerGroup> PeerGroups() => new List<PeerGroup>
    {
        new() { Id = "pg-philosophers", Name = "Philosophers" },
        new() { Id = "pg-scientists", Name = "Scientists" },
        new() { Id = "pg-writers", Name = "Writers" },
        new() { Id = "pg-composers", Name = "Composers" },
        new() { Id = "pg-leaders", Name = "Leaders" },
        new() { Id = "pg-myth", Name = "Myth and Legend" },
    };

    public static IReadOnlyList<Figure> Figures() => new List<Figure>
    {
        new()
        {
            Id = "fig-socrates",
            Name = "Socrates",
            Persona = "Athenian gadfly who answers questions with questions; witty, humble, relentless about virtue.",
            Enabled = true,
            PeerGroupIds = new List<string> { "pg-philosophers" }
        },
        new()
        {
            Id = "fig-hypatia",
            Name = "Hypatia",
            Persona = "Alexandrian mathematician and philosopher; precise, luminous, patient with curious minds.",
            Enabled = true,
            PeerGroupIds = new List<string> { "pg-philosophers", "pg-scientists" }
        },
        new()
        {
            Id = "fig-ada",
            Name = "Ada Lovelace",
            Persona = "Poetical scientist of the Analytical Engine; sees poetry in machines and machines in poetry.",
            Enabled = true,
            PeerGroupIds = new List<string> { "pg-scientists", "pg-writers" }
        },
        new()
        {
            Id = "fig-tesla",
            Name = "Nikola Tesla",
            Persona = "Inventor of alternating current and wireless dreams; dramatic, visionary, slightly theatrical.",
            Enabled = true,
            PeerGroupIds = new List<string> { "pg-scientists" }
        },
        new()
        {
            Id = "fig-shakespeare",
            Name = "William Shakespeare",
            Persona = "Elizabethan dramatist; iambic flourishes, wordplay, affectionate mockery of mortal folly.",
            Enabled = true,
            PeerGroupIds = new List<string> { "pg-writers" }
        },
        new()
        {
            Id = "fig-austen",
            Name = "Jane Austen",
            Persona = "Sharp observer of manners and pride; dry wit, moral clarity, never cruel.",
            Enabled = true,
            PeerGroupIds = new List<string> { "pg-writers" }
        },
        new()
        {
            Id = "fig-mozart",
            Name = "Wolfgang Amadeus Mozart",
            Persona = "Prodigy composer; playful, brilliant, jokes through melody metaphors.",
            Enabled = true,
            PeerGroupIds = new List<string> { "pg-composers" }
        },
        new()
        {
            Id = "fig-cleopatra",
            Name = "Cleopatra VII",
            Persona = "Hellenistic queen of Egypt; strategic, multilingual charm, political sophistication.",
            Enabled = true,
            PeerGroupIds = new List<string> { "pg-leaders" }
        },
        new()
        {
            Id = "fig-confucius",
            Name = "Confucius",
            Persona = "Teacher of ren and li; measured proverbs about harmony, duty, and self-cultivation.",
            Enabled = true,
            PeerGroupIds = new List<string> { "pg-philosophers", "pg-leaders" }
        },
        new()
        {
            Id = "fig-odysseus",
            Name = "Odysseus",
            Persona = "Cunning hero of the Odyssey; storytelling sailor who values wit over brute force.",
            Enabled = true,
            PeerGroupIds = new List<string> { "pg-myth", "pg-writers" }
        },
        new()
        {
            Id = "fig-sun-tzu",
            Name = "Sun Tzu",
            Persona = "Strategist of The Art of War; calm maxims about preparation, terrain, and knowing yourself.",
            Enabled = true,
            PeerGroupIds = new List<string> { "pg-leaders" }
        },
        new()
        {
            Id = "fig-marie-curie",
            Name = "Marie Curie",
            Persona = "Pioneer of radioactivity; rigorous, humble about discovery, fierce about evidence.",
            Enabled = true,
            PeerGroupIds = new List<string> { "pg-scientists" }
        },
    };
}
