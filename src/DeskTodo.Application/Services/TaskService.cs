using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using DeskTodo.Domain.Exceptions;

namespace DeskTodo.Application.Services;

/// <inheritdoc cref="ITaskService"/>
public sealed class TaskService(ITaskRepository taskRepository, ITaskHistoryRepository taskHistoryRepository) : ITaskService
{
    public Task<IReadOnlyList<TaskItem>> GetTasksForDateAsync(DateOnly planDate, CancellationToken cancellationToken = default) =>
        taskRepository.GetByDateAsync(planDate, cancellationToken);

    public Task<IReadOnlyList<TaskItem>> GetAllTasksAsync(CancellationToken cancellationToken = default) =>
        taskRepository.GetAllAsync(cancellationToken);

    public Task<TaskItem?> GetTaskAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        taskRepository.GetByIdAsync(taskId, cancellationToken);

    public async Task<TaskItem> CreateTaskAsync(
        DateOnly planDate,
        string title,
        string? description = null,
        TaskPriority priority = TaskPriority.Medium,
        Guid? categoryId = null,
        DateTime? dueDate = null,
        Guid? parentTaskId = null,
        CancellationToken cancellationToken = default)
    {
        var maxOrder = await taskRepository.GetMaxDayOrderAsync(planDate, cancellationToken);

        var task = new TaskItem
        {
            PlanDate = planDate,
            DayOrder = maxOrder + 1,
            Title = title,
            Description = description,
            Priority = priority,
            CategoryId = categoryId,
            DueDate = dueDate,
            ParentTaskId = parentTaskId,
        };

        await taskRepository.AddAsync(task, cancellationToken);
        await RecordHistoryAsync(task.Id, TaskHistoryAction.Created, cancellationToken: cancellationToken);
        return task;
    }

    public async Task UpdateTaskAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        var before = await taskRepository.GetByIdAsync(task.Id, cancellationToken);
        task.Touch();
        await taskRepository.UpdateAsync(task, cancellationToken);

        if (before is not null)
        {
            await RecordFieldChangesAsync(before, task, cancellationToken);
        }
    }

    public async Task RenameTaskAsync(Guid taskId, string newTitle, CancellationToken cancellationToken = default)
    {
        var task = await GetRequiredAsync(taskId, cancellationToken);
        var oldTitle = task.Title;
        task.Title = newTitle;
        task.Touch();
        await taskRepository.UpdateAsync(task, cancellationToken);
        await RecordIfChangedAsync(taskId, TaskHistoryAction.Renamed, nameof(TaskItem.Title), oldTitle, newTitle, cancellationToken);
    }

    public async Task<TaskItem> DuplicateTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var source = await GetRequiredAsync(taskId, cancellationToken);
        var maxOrder = await taskRepository.GetMaxDayOrderAsync(source.PlanDate, cancellationToken);

        var copy = new TaskItem
        {
            PlanDate = source.PlanDate,
            DayOrder = maxOrder + 1,
            Title = source.Title,
            Description = source.Description,
            Priority = source.Priority,
            CategoryId = source.CategoryId,
            EstimatedMinutes = source.EstimatedMinutes,
            DueDate = source.DueDate,
            Notes = source.Notes,
            ColorHex = source.ColorHex,
        };

        await taskRepository.AddAsync(copy, cancellationToken);
        return copy;
    }

    public async Task CompleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await GetRequiredAsync(taskId, cancellationToken);
        if (task.IsBlocked)
        {
            throw new TaskBlockedException(taskId);
        }

        task.Complete();
        await taskRepository.UpdateAsync(task, cancellationToken);
        await RecordHistoryAsync(taskId, TaskHistoryAction.Completed, cancellationToken: cancellationToken);

        if (task.GetNextOccurrencePlanDate() is not { } nextPlanDate)
        {
            return;
        }

        var maxOrder = await taskRepository.GetMaxDayOrderAsync(nextPlanDate, cancellationToken);
        var nextOccurrence = new TaskItem
        {
            PlanDate = nextPlanDate,
            DayOrder = maxOrder + 1,
            Title = task.Title,
            Description = task.Description,
            Priority = task.Priority,
            CategoryId = task.CategoryId,
            EstimatedMinutes = task.EstimatedMinutes,
            Notes = task.Notes,
            ColorHex = task.ColorHex,
            RecurrenceFrequency = task.RecurrenceFrequency,
            RecurrenceInterval = task.RecurrenceInterval,
            RecurrenceEndDate = task.RecurrenceEndDate,
        };
        await taskRepository.AddAsync(nextOccurrence, cancellationToken);
    }

    public async Task ReopenTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        await MutateAsync(taskId, task => task.Reopen(), cancellationToken);
        await RecordHistoryAsync(taskId, TaskHistoryAction.Reopened, cancellationToken: cancellationToken);
    }

    public Task PinTaskAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        MutateAsync(taskId, task => task.Pin(), cancellationToken);

    public Task UnpinTaskAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        MutateAsync(taskId, task => task.Unpin(), cancellationToken);

    public Task SnoozeTaskAsync(Guid taskId, DateTime until, CancellationToken cancellationToken = default) =>
        MutateAsync(taskId, task => task.Snooze(until), cancellationToken);

    public Task FavoriteTaskAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        MutateAsync(taskId, task => task.MarkFavorite(), cancellationToken);

    public Task UnfavoriteTaskAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        MutateAsync(taskId, task => task.UnmarkFavorite(), cancellationToken);

    public async Task ArchiveTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        await MutateAsync(taskId, task => task.Archive(), cancellationToken);
        await RecordHistoryAsync(taskId, TaskHistoryAction.Archived, cancellationToken: cancellationToken);
    }

    public async Task RestoreTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        await MutateAsync(taskId, task => task.Restore(), cancellationToken);
        await RecordHistoryAsync(taskId, TaskHistoryAction.Restored, cancellationToken: cancellationToken);
    }

    public async Task DeleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        await MutateAsync(taskId, task => task.SoftDelete(), cancellationToken);
        await RecordHistoryAsync(taskId, TaskHistoryAction.Deleted, cancellationToken: cancellationToken);
    }

    public Task<IReadOnlyList<TaskItem>> GetDeletedTasksAsync(CancellationToken cancellationToken = default) =>
        taskRepository.GetDeletedAsync(cancellationToken);

    public Task PermanentlyDeleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        taskRepository.RemoveAsync(taskId, cancellationToken);

    public async Task EmptyTrashAsync(CancellationToken cancellationToken = default)
    {
        var deleted = await taskRepository.GetDeletedAsync(cancellationToken);
        foreach (var task in deleted)
        {
            await taskRepository.RemoveAsync(task.Id, cancellationToken);
        }
    }

    public Task ReorderTasksAsync(DateOnly planDate, IReadOnlyList<Guid> orderedTaskIds, CancellationToken cancellationToken = default) =>
        taskRepository.ReorderAsync(planDate, orderedTaskIds, cancellationToken);

    public Task AddActualMinutesAsync(Guid taskId, int minutes, CancellationToken cancellationToken = default) =>
        MutateAsync(taskId, task => { task.ActualMinutes = (task.ActualMinutes ?? 0) + minutes; task.Touch(); }, cancellationToken);

    public async Task<int> RescheduleOverdueTasksAsync(DateOnly today, CancellationToken cancellationToken = default)
    {
        var overdueTasks = await taskRepository.GetIncompleteBeforeDateAsync(today, cancellationToken);
        if (overdueTasks.Count == 0)
        {
            return 0;
        }

        var nextOrder = await taskRepository.GetMaxDayOrderAsync(today, cancellationToken) + 1;
        foreach (var task in overdueTasks)
        {
            task.PlanDate = today;
            task.DayOrder = nextOrder++;
            task.Touch();
            await taskRepository.UpdateAsync(task, cancellationToken);
        }

        return overdueTasks.Count;
    }

    public Task<IReadOnlyList<TaskHistory>> GetTaskHistoryAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        taskHistoryRepository.GetForTaskAsync(taskId, cancellationToken);

    private async Task MutateAsync(Guid taskId, Action<TaskItem> mutate, CancellationToken cancellationToken)
    {
        var task = await GetRequiredAsync(taskId, cancellationToken);
        mutate(task);
        await taskRepository.UpdateAsync(task, cancellationToken);
    }

    private async Task<TaskItem> GetRequiredAsync(Guid taskId, CancellationToken cancellationToken) =>
        await taskRepository.GetByIdAsync(taskId, cancellationToken) ?? throw new TaskNotFoundException(taskId);

    /// <summary>
    /// The fields the general-purpose editor (<see cref="UpdateTaskAsync"/>) checks for changes.
    /// Deliberately a fixed subset (not every <see cref="TaskItem"/> property) — see
    /// <see cref="TaskHistory"/>'s doc comment on why this stays a curated, high-signal list.
    /// </summary>
    private async Task RecordFieldChangesAsync(TaskItem before, TaskItem after, CancellationToken cancellationToken)
    {
        await RecordIfChangedAsync(after.Id, TaskHistoryAction.Updated, nameof(TaskItem.Title), before.Title, after.Title, cancellationToken);
        await RecordIfChangedAsync(after.Id, TaskHistoryAction.Updated, nameof(TaskItem.Description), before.Description, after.Description, cancellationToken);
        await RecordIfChangedAsync(after.Id, TaskHistoryAction.Updated, nameof(TaskItem.Priority), before.Priority.ToString(), after.Priority.ToString(), cancellationToken);
        await RecordIfChangedAsync(after.Id, TaskHistoryAction.Updated, nameof(TaskItem.DueDate), before.DueDate?.ToString("O"), after.DueDate?.ToString("O"), cancellationToken);
        await RecordIfChangedAsync(after.Id, TaskHistoryAction.Updated, nameof(TaskItem.PlanDate), before.PlanDate.ToString("O"), after.PlanDate.ToString("O"), cancellationToken);
        await RecordIfChangedAsync(after.Id, TaskHistoryAction.Updated, nameof(TaskItem.CategoryId), before.CategoryId?.ToString(), after.CategoryId?.ToString(), cancellationToken);
    }

    private Task RecordIfChangedAsync(Guid taskId, TaskHistoryAction action, string fieldName, string? oldValue, string? newValue, CancellationToken cancellationToken) =>
        oldValue == newValue
            ? Task.CompletedTask
            : RecordHistoryAsync(taskId, action, fieldName, oldValue, newValue, cancellationToken);

    private Task RecordHistoryAsync(
        Guid taskId,
        TaskHistoryAction action,
        string? fieldName = null,
        string? oldValue = null,
        string? newValue = null,
        CancellationToken cancellationToken = default) =>
        taskHistoryRepository.AddAsync(
            new TaskHistory { TaskId = taskId, Action = action, FieldName = fieldName, OldValue = oldValue, NewValue = newValue },
            cancellationToken);
}
