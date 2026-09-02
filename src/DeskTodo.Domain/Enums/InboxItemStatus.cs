namespace DeskTodo.Domain.Enums;

/// <summary>Lifecycle state of an <see cref="Entities.InboxItem"/> — Feature 39, Roadmap-39-100.md.</summary>
public enum InboxItemStatus
{
    /// <summary>Captured but not yet acted on — the default state.</summary>
    Unprocessed = 0,

    /// <summary>Converted into a real <see cref="Entities.TaskItem"/> — see <see cref="Entities.InboxItem.ConvertedTaskId"/>.</summary>
    Converted = 1,

    /// <summary>Kept for reference but no longer needs action — mirrors <see cref="Entities.TaskItem.IsArchived"/>'s "keep, but out of the way" meaning.</summary>
    Archived = 2,
}
