using DeskTodo.Domain.Enums;

namespace DeskTodo.Domain.Entities;

/// <summary>
/// A saved, reusable shape for a task — "New from template" pre-fills a new
/// <see cref="TaskItem"/> from one of these rather than the user re-entering the same
/// title/priority/category/checklist every time for a recurring kind of task (e.g. "Weekly
/// grocery run", "Sprint planning prep").
/// </summary>
public sealed class TaskTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The template's own name in the picker — distinct from <see cref="TaskTitle"/>, the title it seeds onto the created task.</summary>
    public required string Name { get; set; }

    public required string TaskTitle { get; set; }

    public string? Description { get; set; }

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public Guid? CategoryId { get; set; }

    public Category? Category { get; set; }

    public int? EstimatedMinutes { get; set; }

    public string? Notes { get; set; }

    /// <summary>
    /// One checklist line per entry — kept as a simple ordered list rather than its own
    /// relational sub-entity/table, since a template's checklist is only ever copied
    /// wholesale into a new <see cref="ChecklistItem"/> per line when the template is used,
    /// never edited item-by-item the way a real task's checklist is.
    /// </summary>
    public List<string> ChecklistItems { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
