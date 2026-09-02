namespace DeskTodo.Domain.Entities;

/// <summary>Feature 57 (Roadmap-39-100.md) — an important decision recorded independently from ordinary tasks, optionally tied to a project.</summary>
public sealed class Decision
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }

    public required string Title { get; set; }

    /// <summary>Why the decision needed making — background/situation.</summary>
    public string? Context { get; set; }

    /// <summary>What was decided.</summary>
    public required string DecisionText { get; set; }

    /// <summary>Options that were considered, one per line — plain text, not a normalized list; a decision log entry is a note, not a structured comparison table.</summary>
    public string? Alternatives { get; set; }

    /// <summary>Why this option won.</summary>
    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
