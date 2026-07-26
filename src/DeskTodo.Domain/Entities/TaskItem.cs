using DeskTodo.Domain.Enums;

namespace DeskTodo.Domain.Entities;

/// <summary>
/// A single task on a user's daily task list.
/// </summary>
/// <remarks>
/// There is deliberately no single "TaskStatus" enum: Completed, Pinned,
/// Archived and Deleted are independent flags (a task can be pinned *and*
/// completed, archived *and* pinned, etc.), and Overdue is a computed
/// property rather than stored state, since it depends on the current time.
/// Named <c>TaskItem</c> rather than <c>Task</c> to avoid colliding with
/// <see cref="System.Threading.Tasks.Task"/>.
/// </remarks>
public sealed class TaskItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The day this task's list it belongs to. A task is "moved" between
    /// days by changing this value; there is no separate day/plan entity —
    /// a day with zero tasks simply has zero rows with that
    /// <see cref="PlanDate"/>, so "auto-create a day" needs no extra step.
    /// </summary>
    public required DateOnly PlanDate { get; set; }

    /// <summary>
    /// Zero-based position within <see cref="PlanDate"/>'s list. Drives both
    /// the displayed "task number" (position + 1) and drag-to-reorder.
    /// </summary>
    public int DayOrder { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public bool IsCompleted { get; private set; }

    public DateTime? CompletedAt { get; private set; }

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public Guid? CategoryId { get; set; }

    public Category? Category { get; set; }

    public int? EstimatedMinutes { get; set; }

    public int? ActualMinutes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ModifiedAt { get; private set; } = DateTime.UtcNow;

    public DateTime? DueDate { get; set; }

    public string? Notes { get; set; }

    /// <summary>Display color as a "#RRGGBB" hex string; falls back to the category's color when null.</summary>
    public string? ColorHex { get; set; }

    public bool IsPinned { get; private set; }

    public bool IsArchived { get; private set; }

    /// <summary>Soft-delete flag. Deleted tasks are excluded from normal queries but remain recoverable.</summary>
    public bool IsDeleted { get; private set; }

    /// <summary>True when the task has a due date in the past and is not yet completed.</summary>
    public bool IsOverdue => !IsCompleted && DueDate is { } due && due < DateTime.UtcNow;

    public void Complete()
    {
        IsCompleted = true;
        CompletedAt = DateTime.UtcNow;
        Touch();
    }

    /// <summary>Undoes completion (the "Undo completion" feature).</summary>
    public void Reopen()
    {
        IsCompleted = false;
        CompletedAt = null;
        Touch();
    }

    public void Pin()
    {
        IsPinned = true;
        Touch();
    }

    public void Unpin()
    {
        IsPinned = false;
        Touch();
    }

    public void Archive()
    {
        IsArchived = true;
        Touch();
    }

    public void Restore()
    {
        IsArchived = false;
        IsDeleted = false;
        Touch();
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        Touch();
    }

    public void Touch() => ModifiedAt = DateTime.UtcNow;
}
