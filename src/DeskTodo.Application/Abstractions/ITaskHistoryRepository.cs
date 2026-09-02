using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Abstractions;

/// <summary>Persistence abstraction for <see cref="TaskHistory"/> — Feature 42's audit timeline.</summary>
public interface ITaskHistoryRepository
{
    Task AddAsync(TaskHistory entry, CancellationToken cancellationToken = default);

    /// <summary>A task's history entries, most recent first.</summary>
    Task<IReadOnlyList<TaskHistory>> GetForTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>Every history entry ever recorded, most recent first — raw material for Feature 61's Activity Timeline.</summary>
    Task<IReadOnlyList<TaskHistory>> GetAllAsync(CancellationToken cancellationToken = default);
}
