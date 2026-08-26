using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Abstractions;

/// <summary>Persistence abstraction for <see cref="TaskHistory"/> — Feature 42's audit timeline.</summary>
public interface ITaskHistoryRepository
{
    Task AddAsync(TaskHistory entry, CancellationToken cancellationToken = default);

    /// <summary>A task's history entries, most recent first.</summary>
    Task<IReadOnlyList<TaskHistory>> GetForTaskAsync(Guid taskId, CancellationToken cancellationToken = default);
}
