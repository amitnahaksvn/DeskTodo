namespace DeskTodo.Domain.Entities;

/// <summary>
/// A named, user-defined set of <see cref="TaskTemplate"/>s that get created together, in
/// one click, rather than one at a time — e.g. a "Morning Routine" group bundling several
/// habit-style templates. Deliberately references existing templates by id (see
/// <see cref="TemplateIds"/>) rather than duplicating each member's title/priority/checklist
/// onto the group itself — a group is "a saved combination of templates," not a second place
/// a task's shape gets defined.
/// </summary>
public sealed class TaskGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    /// <summary>
    /// Ordered ids of the member <see cref="TaskTemplate"/>s — order is preserved so the
    /// group's own list (and the day-order of the tasks created from it) matches how the
    /// user arranged them, not an arbitrary database order.
    /// </summary>
    public List<Guid> TemplateIds { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
