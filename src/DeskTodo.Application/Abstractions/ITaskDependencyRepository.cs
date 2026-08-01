using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Abstractions;

/// <summary>Persistence abstraction for <see cref="TaskDependency"/> rows.</summary>
public interface ITaskDependencyRepository
{
    /// <summary>The tasks blocking <paramref name="taskId"/> — includes each <see cref="TaskDependency.BlockingTask"/>.</summary>
    Task<IReadOnlyList<TaskDependency>> GetBlockersForTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    Task AddAsync(TaskDependency dependency, CancellationToken cancellationToken = default);

    /// <summary>No-ops if the dependency doesn't exist.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid blockingTaskId, Guid blockedTaskId, CancellationToken cancellationToken = default);
}
