namespace DeskTodo.Domain.Entities;

/// <summary>
/// Records that <see cref="BlockedTask"/> can't be completed until
/// <see cref="BlockingTask"/> is. A plain join entity (rather than a many-to-many skip
/// navigation, like <see cref="Tag"/> uses) because the two sides mean different things —
/// "blocks" and "is blocked by" aren't interchangeable the way "has this tag" is
/// symmetric, so each direction needs its own named navigation on <see cref="TaskItem"/>.
/// </summary>
public sealed class TaskDependency
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The prerequisite — must be completed first.</summary>
    public required Guid BlockingTaskId { get; set; }

    public TaskItem? BlockingTask { get; set; }

    /// <summary>The task waiting on <see cref="BlockingTask"/>.</summary>
    public required Guid BlockedTaskId { get; set; }

    public TaskItem? BlockedTask { get; set; }
}
