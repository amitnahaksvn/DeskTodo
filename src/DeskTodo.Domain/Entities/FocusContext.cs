namespace DeskTodo.Domain.Entities;

/// <summary>
/// Feature 63 (Roadmap-39-100.md) — a cross-cutting "which part of my life is this" label
/// (Work/Personal/Learning/...), deliberately distinct from <see cref="Project"/> (an ongoing
/// deliverable bucket) — a task can carry both at once, e.g. Project "Side Project Website" +
/// Context "Side Project", per the spec's own example.
/// </summary>
public sealed class FocusContext
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    /// <summary>Display color as a "#RRGGBB" hex string, same format every other colored grouping (Category/Project/Tag) in this app uses.</summary>
    public required string ColorHex { get; set; }

    public ICollection<TaskItem> Tasks { get; set; } = [];
}
