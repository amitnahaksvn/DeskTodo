using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Abstractions;

/// <summary>Persistence abstraction for <see cref="Project"/>. Each method is a self-contained unit of work — see the remarks on <see cref="ITaskRepository"/>.</summary>
public interface IProjectRepository
{
    /// <summary>Includes each project's linked <see cref="Project.Tasks"/> — needed for the "X/Y tasks done" progress the Projects tab shows.</summary>
    Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Project project, CancellationToken cancellationToken = default);

    Task UpdateAsync(Project project, CancellationToken cancellationToken = default);

    /// <summary>No-ops if the project doesn't exist. Linked tasks are unlinked (ProjectId set null), not deleted — see TaskItemConfiguration's SetNull.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
