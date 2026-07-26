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

    Task<TaskItem> CreateTaskAsync(
        DateOnly planDate,
        string title,
        string? description = null,
        TaskPriority priority = TaskPriority.Medium,
        Guid? categoryId = null,
        DateTime? dueDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>Persists edits already applied to a fetched <see cref="TaskItem"/> (title, notes, priority, etc.).</summary>
    Task UpdateTaskAsync(TaskItem task, CancellationToken cancellationToken = default);

    Task<TaskItem> DuplicateTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    Task CompleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>Undoes completion.</summary>
    Task ReopenTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    Task PinTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    Task UnpinTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    Task ArchiveTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    Task RestoreTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes a task (recoverable via <see cref="RestoreTaskAsync"/>).</summary>
    Task DeleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>Applies a new drag-to-reorder sequence for a day's task list.</summary>
    Task ReorderTasksAsync(DateOnly planDate, IReadOnlyList<Guid> orderedTaskIds, CancellationToken cancellationToken = default);
}
