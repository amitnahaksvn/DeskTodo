using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Abstractions;

/// <summary>
/// Persistence abstraction for <see cref="TaskTemplate"/>. Each method is a
/// self-contained unit of work — see the remarks on <see cref="ITaskRepository"/>.
/// </summary>
public interface ITaskTemplateRepository
{
    Task<IReadOnlyList<TaskTemplate>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TaskTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(TaskTemplate template, CancellationToken cancellationToken = default);

    /// <summary>No-ops if the template doesn't exist.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
