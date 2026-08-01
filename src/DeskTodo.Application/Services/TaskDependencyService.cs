using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Services;

/// <inheritdoc cref="ITaskDependencyService"/>
public sealed class TaskDependencyService(ITaskDependencyRepository dependencyRepository) : ITaskDependencyService
{
    public Task<IReadOnlyList<TaskDependency>> GetBlockersAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        dependencyRepository.GetBlockersForTaskAsync(taskId, cancellationToken);

    public async Task AddBlockerAsync(Guid blockedTaskId, Guid blockingTaskId, CancellationToken cancellationToken = default)
    {
        if (blockedTaskId == blockingTaskId)
        {
            return;
        }

        if (await dependencyRepository.ExistsAsync(blockingTaskId, blockedTaskId, cancellationToken))
        {
            return;
        }

        if (await dependencyRepository.ExistsAsync(blockedTaskId, blockingTaskId, cancellationToken))
        {
            return;
        }

        await dependencyRepository.AddAsync(
            new TaskDependency { BlockingTaskId = blockingTaskId, BlockedTaskId = blockedTaskId },
            cancellationToken);
    }

    public Task RemoveBlockerAsync(Guid dependencyId, CancellationToken cancellationToken = default) =>
        dependencyRepository.DeleteAsync(dependencyId, cancellationToken);
}
