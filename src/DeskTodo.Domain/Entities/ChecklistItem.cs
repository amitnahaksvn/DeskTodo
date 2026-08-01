namespace DeskTodo.Domain.Entities;

/// <summary>
/// A single checkable line inside a <see cref="TaskItem"/>'s checklist — lighter-weight
/// than a full subtask (no due date, priority or category of its own) by design, since a
/// checklist item's only job is "did I do this small thing yet."
/// </summary>
public sealed class ChecklistItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required Guid TaskId { get; set; }

    public TaskItem? Task { get; set; }

    public required string Text { get; set; }

    public bool IsChecked { get; set; }

    /// <summary>Zero-based position within the owning task's checklist — same drag-to-reorder-by-index convention as <see cref="TaskItem.DayOrder"/>.</summary>
    public int Order { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
