namespace DeskTodo.Domain.Entities;

/// <summary>
/// A free-form, user-created, multi-valued label a task can carry — distinct from
/// <see cref="Category"/>, which is single-valued and exclusive per task. A task can have
/// any number of tags; a tag can be on any number of tasks (many-to-many).
/// </summary>
public sealed class Tag
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    public ICollection<TaskItem> Tasks { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
