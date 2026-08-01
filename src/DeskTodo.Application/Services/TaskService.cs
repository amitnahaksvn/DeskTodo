using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using DeskTodo.Domain.Exceptions;

namespace DeskTodo.Application.Services;

/// <inheritdoc cref="ITaskService"/>
public sealed class TaskService(ITaskRepository taskRepository) : ITaskService
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
        return task;
    }

    public async Task UpdateTaskAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        task.Touch();
        await taskRepository.UpdateAsync(task, cancellationToken);
    }

    public Task RenameTaskAsync(Guid taskId, string newTitle, CancellationToken cancellationToken = default) =>
        MutateAsync(taskId, task => { task.Title = newTitle; task.Touch(); }, cancellationToken);

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

    public Task ReopenTaskAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        MutateAsync(taskId, task => task.Reopen(), cancellationToken);

    public Task PinTaskAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        MutateAsync(taskId, task => task.Pin(), cancellationToken);

    public Task UnpinTaskAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        MutateAsync(taskId, task => task.Unpin(), cancellationToken);

    public Task FavoriteTaskAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        MutateAsync(taskId, task => task.MarkFavorite(), cancellationToken);

    public Task UnfavoriteTaskAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        MutateAsync(taskId, task => task.UnmarkFavorite(), cancellationToken);

    public Task ArchiveTaskAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        MutateAsync(taskId, task => task.Archive(), cancellationToken);

    public Task RestoreTaskAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        MutateAsync(taskId, task => task.Restore(), cancellationToken);

    public Task DeleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        MutateAsync(taskId, task => task.SoftDelete(), cancellationToken);

    public Task ReorderTasksAsync(DateOnly planDate, IReadOnlyList<Guid> orderedTaskIds, CancellationToken cancellationToken = default) =>
        taskRepository.ReorderAsync(planDate, orderedTaskIds, cancellationToken);

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

    private async Task MutateAsync(Guid taskId, Action<TaskItem> mutate, CancellationToken cancellationToken)
    {
        var task = await GetRequiredAsync(taskId, cancellationToken);
        mutate(task);
        await taskRepository.UpdateAsync(task, cancellationToken);
    }

    private async Task<TaskItem> GetRequiredAsync(Guid taskId, CancellationToken cancellationToken) =>
        await taskRepository.GetByIdAsync(taskId, cancellationToken) ?? throw new TaskNotFoundException(taskId);
}
