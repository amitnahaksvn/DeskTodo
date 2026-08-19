using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Abstractions;

/// <summary>
/// Persistence abstraction for <see cref="TaskGroup"/>. Each method is a self-contained
/// unit of work — see the remarks on <see cref="ITaskRepository"/>.
/// </summary>
public interface ITaskGroupRepository
{
    Task<IReadOnlyList<TaskGroup>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TaskGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(TaskGroup group, CancellationToken cancellationToken = default);

    Task UpdateAsync(TaskGroup group, CancellationToken cancellationToken = default);

    /// <summary>No-ops if the group doesn't exist.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
