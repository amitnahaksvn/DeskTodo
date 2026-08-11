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

    /// <summary>What kind of activity this is (Task/Event/Reminder/Note/Meeting) — see <see cref="Enums.TaskType"/>. Defaults to the plain <see cref="Enums.TaskType.Task"/> so every existing/imported row is unaffected.</summary>
    public TaskType Type { get; set; } = TaskType.Task;

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

    /// <summary>
    /// A second, independent boolean flag from <see cref="IsPinned"/> — Pin means "keep at
    /// the top of today's list"; Favorite means "important to me across every day" (e.g. a
    /// long-running goal-adjacent task). Deliberately not unified into one flag since they
    /// answer different questions and a task can reasonably be either, both, or neither.
    /// </summary>
    public bool IsFavorite { get; private set; }

    public ICollection<ChecklistItem> ChecklistItems { get; set; } = [];

    public ICollection<Tag> Tags { get; set; } = [];

    public ICollection<Attachment> Attachments { get; set; } = [];

    /// <summary>
    /// A lighter-weight, single-level parent/child relationship — deliberately not a
    /// general tree (a subtask having its own subtasks isn't supported at the UI layer,
    /// even though nothing at this layer technically prevents it). Distinct from
    /// <see cref="ChecklistItems"/>: a subtask is a full <see cref="TaskItem"/> with its
    /// own priority/due date/etc., for when a checklist line is too lightweight.
    /// </summary>
    public Guid? ParentTaskId { get; set; }

    public TaskItem? ParentTask { get; set; }

    public ICollection<TaskItem> Subtasks { get; set; } = [];

    /// <summary>Optional link to the project-style milestone this task contributes to — see <see cref="Entities.Milestone"/>. Null means "not part of a milestone," the common case.</summary>
    public Guid? MilestoneId { get; set; }

    public Milestone? Milestone { get; set; }

    /// <summary>Optional link to the ongoing project bucket this task belongs to — see <see cref="Entities.Project"/>. Distinct from <see cref="MilestoneId"/> (a fixed deliverable) and <see cref="CategoryId"/> (a lighter day-to-day label); a task can carry any combination of the three.</summary>
    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }

    /// <summary>Tasks that must be completed before this one can be — see <see cref="Entities.TaskDependency"/>.</summary>
    public ICollection<TaskDependency> BlockedByDependencies { get; set; } = [];

    /// <summary>Tasks that are waiting on this one — the inverse side of <see cref="BlockedByDependencies"/>.</summary>
    public ICollection<TaskDependency> BlockingDependencies { get; set; } = [];

    /// <summary>
    /// <see cref="Enums.RecurrenceFrequency.None"/> (the default) means this task doesn't
    /// recur. A non-None value means completing this task creates its next occurrence —
    /// see <see cref="GetNextOccurrencePlanDate"/>.
    /// </summary>
    public RecurrenceFrequency RecurrenceFrequency { get; set; } = RecurrenceFrequency.None;

    /// <summary>"Every N" — e.g. <see cref="RecurrenceFrequency.Weekly"/> with Interval 2 means every other week. Meaningless when <see cref="RecurrenceFrequency"/> is None.</summary>
    public int RecurrenceInterval { get; set; } = 1;

    /// <summary>Optional cutoff — once the *next* occurrence's date would fall after this, no further occurrences are created. Null means "recur forever."</summary>
    public DateOnly? RecurrenceEndDate { get; set; }

    /// <summary>True when the task has a due date in the past and is not yet completed.</summary>
    public bool IsOverdue => !IsCompleted && DueDate is { } due && due < DateTime.UtcNow;

    /// <summary>
    /// True when any of <see cref="BlockedByDependencies"/>' blocking tasks is still
    /// incomplete. Requires <see cref="TaskDependency.BlockingTask"/> to have been loaded
    /// (e.g. via <c>ITaskRepository.GetByIdAsync</c>'s include) — evaluates to <c>false</c>
    /// (not blocked) rather than throwing if it wasn't, since an un-loaded navigation is
    /// just an empty collection, not an error state.
    /// </summary>
    public bool IsBlocked => BlockedByDependencies.Any(d => d.BlockingTask is { IsCompleted: false });

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

    public void MarkFavorite()
    {
        IsFavorite = true;
        Touch();
    }

    public void UnmarkFavorite()
    {
        IsFavorite = false;
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

    /// <summary>
    /// The <see cref="PlanDate"/> this task's next occurrence should land on, computed from
    /// <see cref="RecurrenceFrequency"/>/<see cref="RecurrenceInterval"/> — or null when this
    /// task doesn't recur, or the next occurrence would fall after
    /// <see cref="RecurrenceEndDate"/>. Pure computation, no side effects; the caller
    /// (<c>TaskService.CompleteTaskAsync</c>) is responsible for actually creating the next
    /// occurrence's row.
    /// </summary>
    public DateOnly? GetNextOccurrencePlanDate()
    {
        if (RecurrenceFrequency == RecurrenceFrequency.None)
        {
            return null;
        }

        var interval = Math.Max(1, RecurrenceInterval);
        var next = RecurrenceFrequency switch
        {
            RecurrenceFrequency.Daily => PlanDate.AddDays(interval),
            RecurrenceFrequency.Weekly => PlanDate.AddDays(7 * interval),
            RecurrenceFrequency.Monthly => PlanDate.AddMonths(interval),
            _ => (DateOnly?)null,
        };

        if (next is null || (RecurrenceEndDate is { } end && next > end))
        {
            return null;
        }

        return next;
    }
}
