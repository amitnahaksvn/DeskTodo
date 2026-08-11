namespace DeskTodo.Domain.Entities;

/// <summary>
/// An ongoing, color-coded container for related tasks (e.g. "Website Redesign", "Q3
/// Planning") — distinct from <see cref="Category"/> (a lighter, often built-in day-to-day
/// label with no archive state) and <see cref="Milestone"/> (a fixed deliverable with a
/// target date, not an ongoing bucket). A task can belong to a Project, a Category, both,
/// or neither. Deliberately flat, same reasoning as <see cref="Milestone"/> — no
/// parent/child nesting ("Folders").
/// </summary>
public sealed class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    public string? Description { get; set; }

    /// <summary>Display color as a "#RRGGBB" hex string.</summary>
    public required string ColorHex { get; set; }

    /// <summary>Explicitly settable, not derived — same "the user says so" reasoning as <see cref="Milestone.IsCompleted"/>.</summary>
    public bool IsArchived { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TaskItem> Tasks { get; set; } = [];
}
