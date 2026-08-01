using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Services;

/// <summary>Task Dependency use cases: list a task's blockers, add/remove a "blocked by" relationship.</summary>
public interface ITaskDependencyService
{
    /// <summary>The tasks blocking <paramref name="taskId"/> — see <see cref="TaskItem.BlockedByDependencies"/>.</summary>
    Task<IReadOnlyList<TaskDependency>> GetBlockersAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records that <paramref name="blockedTaskId"/> can't be completed until
    /// <paramref name="blockingTaskId"/> is. A no-op (not an error) if the two IDs are the
    /// same, the relationship already exists, or recording it would create a direct
    /// two-task cycle (A blocks B while B already blocks A) — deeper, transitive cycles
    /// (A→B→C→A) aren't detected.
    /// </summary>
    Task AddBlockerAsync(Guid blockedTaskId, Guid blockingTaskId, CancellationToken cancellationToken = default);

    Task RemoveBlockerAsync(Guid dependencyId, CancellationToken cancellationToken = default);
}
