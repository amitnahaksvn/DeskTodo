using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Abstractions;

public interface IProjectTemplateRepository
{
    Task<IReadOnlyList<ProjectTemplate>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ProjectTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(ProjectTemplate template, CancellationToken cancellationToken = default);

    Task UpdateAsync(ProjectTemplate template, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
