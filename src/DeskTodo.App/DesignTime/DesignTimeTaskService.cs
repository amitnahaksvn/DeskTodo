using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;

namespace DeskTodo.App.DesignTime;

/// <summary>
/// No-op <see cref="ITaskService"/> used only as a fallback when
/// <see cref="App.Services"/> is null — i.e. at XAML-designer time, which
/// never runs through <c>Program.Main</c>'s DI container.
/// </summary>
internal sealed class DesignTimeTaskService : ITaskService
{
    public Task<IReadOnlyList<TaskItem>> GetTasksForDateAsync(DateOnly planDate, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TaskItem>>([]);

    public Task<IReadOnlyList<TaskItem>> GetAllTasksAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TaskItem>>([]);

    public Task<TaskItem?> GetTaskAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        Task.FromResult<TaskItem?>(null);

    public Task<TaskItem> CreateTaskAsync(
        DateOnly planDate,
        string title,
        string? description = null,
        TaskPriority priority = TaskPriority.Medium,
        Guid? categoryId = null,
        DateTime? dueDate = null,
        Guid? parentTaskId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new TaskItem { PlanDate = planDate, Title = title });

    public Task UpdateTaskAsync(TaskItem task, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task RenameTaskAsync(Guid taskId, string newTitle, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<TaskItem> DuplicateTaskAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new TaskItem { PlanDate = DateOnly.FromDateTime(DateTime.Now), Title = string.Empty });

    public Task CompleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task ReopenTaskAsync(Guid taskId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task PinTaskAsync(Guid taskId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task UnpinTaskAsync(Guid taskId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task FavoriteTaskAsync(Guid taskId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task UnfavoriteTaskAsync(Guid taskId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task ArchiveTaskAsync(Guid taskId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task RestoreTaskAsync(Guid taskId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task DeleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task ReorderTasksAsync(DateOnly planDate, IReadOnlyList<Guid> orderedTaskIds, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<int> RescheduleOverdueTasksAsync(DateOnly today, CancellationToken cancellationToken = default) =>
        Task.FromResult(0);

    public Task AddActualMinutesAsync(Guid taskId, int minutes, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
