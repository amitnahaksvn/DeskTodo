namespace DeskTodo.Domain.Entities;

/// <summary>Feature 60 (Roadmap-39-100.md) — a date-based personal/work journal entry. Deliberately not a task ("Do not turn the journal into another task list" — the spec's own note) — no priority/due date/completion state, just a date, title, free text and an optional mood.</summary>
public sealed class JournalEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required DateOnly Date { get; set; }

    public required string Title { get; set; }

    public required string Content { get; set; }

    /// <summary>Free-form, optional — e.g. an emoji or short word. No fixed vocabulary; this is a personal note, not a structured mood-tracking feature.</summary>
    public string? Mood { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
