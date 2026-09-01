using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;

namespace DeskTodo.Application.Services;

/// <summary>
/// Task use cases: creation, editing and the state-toggle operations
/// (complete/reopen/pin/archive/delete/duplicate/reorder). Encapsulates the
/// business rules — timestamps, day-order assignment — that ViewModels
/// shouldn't need to know about.
/// </summary>
public interface ITaskService
{
    Task<IReadOnlyList<TaskItem>> GetTasksForDateAsync(DateOnly planDate, CancellationToken cancellationToken = default);

    /// <summary>Every non-deleted task, across every day — for export.</summary>
    Task<IReadOnlyList<TaskItem>> GetAllTasksAsync(CancellationToken cancellationToken = default);

    /// <summary>Fetches a single task for the full-field editor to populate its form from.</summary>
    Task<TaskItem?> GetTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    Task<TaskItem> CreateTaskAsync(
        DateOnly planDate,
        string title,
        string? description = null,
        TaskPriority priority = TaskPriority.Medium,
        Guid? categoryId = null,
        DateTime? dueDate = null,
        Guid? parentTaskId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Persists edits already applied to a fetched <see cref="TaskItem"/> (title, notes, priority, etc.).</summary>
    Task UpdateTaskAsync(TaskItem task, CancellationToken cancellationToken = default);

    /// <summary>Renames a task in place — used by the widget's inline (double-click) title edit, which doesn't hold a full fetched <see cref="TaskItem"/>.</summary>
    Task RenameTaskAsync(Guid taskId, string newTitle, CancellationToken cancellationToken = default);

    Task<TaskItem> DuplicateTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes the task. If it's recurring (<see cref="TaskItem.RecurrenceFrequency"/> isn't
    /// <see cref="RecurrenceFrequency.None"/>), also creates its next occurrence — see
    /// <see cref="TaskItem.GetNextOccurrencePlanDate"/>. Throws
    /// <see cref="Domain.Exceptions.TaskBlockedException"/> if <see cref="TaskItem.IsBlocked"/>.
    /// </summary>
    Task CompleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>Undoes completion.</summary>
    Task ReopenTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    Task PinTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    Task UnpinTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>Suppresses re-notifying this task's overdue alert until <paramref name="until"/> — see <see cref="Domain.Entities.TaskItem.SnoozedUntil"/>.</summary>
    Task SnoozeTaskAsync(Guid taskId, DateTime until, CancellationToken cancellationToken = default);

    Task FavoriteTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    Task UnfavoriteTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    Task ArchiveTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    Task RestoreTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes a task (recoverable via <see cref="RestoreTaskAsync"/>).</summary>
    Task DeleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>Feature 46's Trash view — every soft-deleted task, most recently deleted first.</summary>
    Task<IReadOnlyList<TaskItem>> GetDeletedTasksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Hard delete — permanently and irreversibly removes the task (and its checklist items,
    /// attachments, tag links, dependency links; any subtasks are orphaned, not deleted — see
    /// <see cref="Abstractions.ITaskRepository.RemoveAsync"/>). Unlike <see cref="DeleteTaskAsync"/>,
    /// there is no undo. Only Feature 46's Trash view should call this.
    /// </summary>
    Task PermanentlyDeleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>Permanently deletes every currently soft-deleted task — Trash's "Empty Trash" action. Same irreversibility as <see cref="PermanentlyDeleteTaskAsync"/>, applied to all of them at once.</summary>
    Task EmptyTrashAsync(CancellationToken cancellationToken = default);

    /// <summary>Applies a new drag-to-reorder sequence for a day's task list.</summary>
    Task ReorderTasksAsync(DateOnly planDate, IReadOnlyList<Guid> orderedTaskIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds <paramref name="minutes"/> onto <see cref="TaskItem.ActualMinutes"/> (starting from
    /// 0 if it was never set) — backs Phase 23's Time Tracking, called when a focus session
    /// linked to this task completes. Additive, not a replace, so multiple sessions against
    /// the same task accumulate rather than each overwriting the last.
    /// </summary>
    Task AddActualMinutesAsync(Guid taskId, int minutes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bumps every incomplete, non-archived, non-deleted task whose <see cref="TaskItem.PlanDate"/>
    /// is before <paramref name="today"/> to land on <paramref name="today"/> instead (appended to
    /// the end of that day's list). Backs the "auto-reschedule overdue tasks" setting. Returns how
    /// many tasks were moved.
    /// </summary>
    Task<int> RescheduleOverdueTasksAsync(DateOnly today, CancellationToken cancellationToken = default);

    /// <summary>Feature 42's audit timeline for a single task, most recent first.</summary>
    Task<IReadOnlyList<TaskHistory>> GetTaskHistoryAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>Feature 44's version snapshots for a single task, most recent first.</summary>
    Task<IReadOnlyList<TaskVersion>> GetTaskVersionsAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Overwrites the task's editable fields (title/description/priority/category/due
    /// date/notes/color/estimated minutes) with a previously captured <see cref="TaskVersion"/>'s
    /// values. The task's state *before* the restore is itself captured as a new version first,
    /// so a restore is never a one-way trip — restoring again just goes back to whichever version
    /// is picked next.
    /// </summary>
    Task RestoreTaskVersionAsync(Guid taskId, Guid versionId, CancellationToken cancellationToken = default);
}
